using GameStop.Web.Data;
using GameStop.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStop.Web.Controllers;

public class GamesController(AppDbContext db, FunctionService functions) : Controller
{
    public async Task<IActionResult> Index(string? search, string? platform)
    {
        var query = db.Games.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(g => g.Title.Contains(search) || g.Genre.Contains(search));

        if (!string.IsNullOrWhiteSpace(platform))
            query = query.Where(g => g.Platform.Contains(platform));

        ViewBag.Search = search;
        ViewBag.Platform = platform;
        ViewBag.Platforms = await db.Games.Select(g => g.Platform).Distinct().OrderBy(p => p).ToListAsync();

        return View(await query.OrderBy(g => g.Title).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var game = await db.Games.FindAsync(id);
        if (game is null) return NotFound();
        return View(game);
    }

    [HttpGet("/Games/Stock")]
    public async Task<IActionResult> Stock(int gameId)
    {
        var result = await functions.GetGameStockAsync(gameId);
        if (result is null)
        {
            // fallback to DB if function is unavailable
            var game = await db.Games.FindAsync(gameId);
            if (game is null) return NotFound();
            var status = game.StockCount == 0 ? "OutOfStock" : game.StockCount <= 10 ? "LowStock" : "InStock";
            return Json(new { gameId, game.Title, game.StockCount, status });
        }
        return Json(result);
    }
}
