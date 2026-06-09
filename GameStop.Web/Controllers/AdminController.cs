using Azure.Data.Tables;
using GameStop.Web.Data;
using GameStop.Web.Filters;
using GameStop.Web.Models;
using GameStop.Web.Models.ViewModels;
using GameStop.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStop.Web.Controllers;

[AdminOnly]
public class AdminController(AppDbContext db, BlobStorageService blob, IConfiguration config) : Controller
{
    private async Task WriteAuditLog(string action, string detail)
    {
        try
        {
            var connStr = config["AzureStorage:ConnectionString"];
            var tableName = config["AzureStorage:AuditTable"] ?? "AuditLogs";
            var tableClient = new TableClient(connStr, tableName);
            await tableClient.CreateIfNotExistsAsync();
            var entity = new TableEntity("AdminAction", Guid.NewGuid().ToString())
            {
                ["Action"]      = action,
                ["Detail"]      = detail,
                ["PerformedBy"] = HttpContext.Session.GetString("Username") ?? "admin",
                ["Timestamp"]   = DateTime.UtcNow.ToString("o")
            };
            await tableClient.AddEntityAsync(entity);
        }
        catch { /* non-critical */ }
    }

    public async Task<IActionResult> Dashboard()
    {
        var games = await db.Games.ToListAsync();

        ViewBag.TotalGames     = games.Count;
        ViewBag.TotalStock     = games.Sum(g => g.StockCount);
        ViewBag.LowStock       = games.Count(g => g.StockCount > 0 && g.StockCount <= 10);
        ViewBag.OutOfStock     = games.Count(g => g.StockCount == 0);
        ViewBag.InStock        = games.Count(g => g.StockCount > 10);
        ViewBag.AvgPrice       = games.Any() ? games.Average(g => g.Price) : 0;
        ViewBag.InventoryValue = games.Sum(g => g.Price * g.StockCount);
        ViewBag.LowStockGames  = games.Where(g => g.StockCount > 0 && g.StockCount <= 10).OrderBy(g => g.StockCount).ToList();
        ViewBag.TopStockGames  = games.OrderByDescending(g => g.StockCount).Take(5).ToList();

        var recentLogs = new List<Azure.Data.Tables.TableEntity>();
        try
        {
            var tableClient = new Azure.Data.Tables.TableClient(config["AzureStorage:ConnectionString"], config["AzureStorage:AuditTable"] ?? "AuditLogs");
            await foreach (var e in tableClient.QueryAsync<Azure.Data.Tables.TableEntity>())
                recentLogs.Add(e);
        }
        catch { }
        ViewBag.RecentLogs = recentLogs.OrderByDescending(e => e.Timestamp).Take(5).ToList();

        return View();
    }

    public async Task<IActionResult> Inventory()
    {
        var games = await db.Games.OrderBy(g => g.Title).ToListAsync();
        return View(games);
    }

    public async Task<IActionResult> Orders()
    {
        var orders = new List<TableEntity>();
        try
        {
            var tableClient = new TableClient(config["AzureStorage:ConnectionString"], config["AzureStorage:AuditTable"] ?? "AuditLogs");
            await foreach (var e in tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == "Order"))
                orders.Add(e);
        }
        catch { }

        var sorted = orders.OrderByDescending(e => e.Timestamp).ToList();

        // enrich with game titles
        var gameIds = sorted
            .Where(e => e.TryGetValue("GameId", out _))
            .Select(e => (int)(e["GameId"] as int? ?? 0))
            .Distinct()
            .ToList();
        var games = await db.Games
            .Where(g => gameIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Title);
        ViewBag.Games = games;

        return View(sorted);
    }

    public async Task<IActionResult> AuditLog()
    {
        var logs = new List<TableEntity>();
        try
        {
            var connStr     = config["AzureStorage:ConnectionString"];
            var tableName   = config["AzureStorage:AuditTable"] ?? "AuditLogs";
            var tableClient = new TableClient(connStr, tableName);
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
                logs.Add(entity);
        }
        catch { /* table may not exist yet */ }

        var sorted = logs
            .OrderByDescending(e => e.ContainsKey("Action"))
            .ThenByDescending(e => e.Timestamp)
            .ToList();
        return View(sorted);
    }

    [HttpGet]
    public IActionResult CreateGame() => View(new GameFormViewModel());

    [HttpPost]
    public async Task<IActionResult> CreateGame(GameFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string imageUrl = "/images/placeholder.png";
        if (model.ImageFile is { Length: > 0 })
        {
            await using var stream = model.ImageFile.OpenReadStream();
            imageUrl = await blob.UploadImageAsync(stream, model.ImageFile.FileName, model.ImageFile.ContentType);
        }

        db.Games.Add(new Game
        {
            Title       = model.Title,
            Platform    = model.Platform,
            Genre       = model.Genre,
            Description = model.Description,
            Price       = model.Price,
            StockCount  = model.StockCount,
            ImageUrl    = imageUrl,
            CreatedAt   = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        await WriteAuditLog("GameCreated", $"Added '{model.Title}' ({model.Platform})");
        return RedirectToAction("Inventory");
    }

    [HttpGet]
    public async Task<IActionResult> EditGame(int id)
    {
        var game = await db.Games.FindAsync(id);
        if (game is null) return NotFound();
        return View(new GameFormViewModel
        {
            Id               = game.Id,
            Title            = game.Title,
            Platform         = game.Platform,
            Genre            = game.Genre,
            Description      = game.Description,
            Price            = game.Price,
            StockCount       = game.StockCount,
            ExistingImageUrl = game.ImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditGame(GameFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var game = await db.Games.FindAsync(model.Id);
        if (game is null) return NotFound();

        game.Title       = model.Title;
        game.Platform    = model.Platform;
        game.Genre       = model.Genre;
        game.Description = model.Description;
        game.Price       = model.Price;
        game.StockCount  = model.StockCount;

        if (model.ImageFile is { Length: > 0 })
        {
            await using var stream = model.ImageFile.OpenReadStream();
            game.ImageUrl = await blob.UploadImageAsync(stream, model.ImageFile.FileName, model.ImageFile.ContentType);
        }

        await db.SaveChangesAsync();
        await WriteAuditLog("GameUpdated", $"Edited '{game.Title}' ({game.Platform})");
        return RedirectToAction("Inventory");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGame(int id)
    {
        var game = await db.Games.FindAsync(id);
        if (game is not null)
        {
            var title = game.Title;
            db.Games.Remove(game);
            await db.SaveChangesAsync();
            await WriteAuditLog("GameDeleted", $"Deleted '{title}'");
        }
        return RedirectToAction("Inventory");
    }
}
