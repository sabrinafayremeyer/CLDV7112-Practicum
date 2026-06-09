using GameStop.Web.Models;
using GameStop.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace GameStop.Web.Data;

public static class DataSeeder
{
    private record SeedGame(string FileName, string Title, string Platform, string Genre, string Description, decimal Price, int Stock);

    private static readonly SeedGame[] Games =
    [
        new("alicemadness.jpg",  "Alice: Madness Returns",          "PC",                "Action / Adventure", "Follow Alice into a dark reimagining of Wonderland in this twisted action-platformer.",             299m,  45),
        new("dragonage.jpg",     "Dragon Age: Origins",             "PC / Xbox 360",     "RPG",                "An epic dark fantasy RPG where your choices shape the fate of an entire world.",                  399m,  32),
        new("hellokitty.avif",   "Hello Kitty Island Adventure",    "PC / Switch",       "Simulation",         "Build a paradise island resort with Hello Kitty and friends in this cosy life sim.",              549m,  18),
        new("hogwarts.jpg",      "Hogwarts Legacy",                  "Multi-platform",    "Action / RPG",       "Live the unwritten — explore the wizarding world before the events of Harry Potter.",            899m,  67),
        new("marvelrivals.png",  "Marvel Rivals",                   "PC / PS5",          "Hero Shooter",       "A 6v6 hero shooter set in the Marvel Universe. Assemble your squad and dominate.",               749m,  54),
        new("poe2.jpg",          "Path of Exile 2",                 "PC",                "Action / RPG",       "The massive free-to-play dark fantasy ARPG sequel — thousands of build possibilities.",           0m,    999),
        new("r6.jpg",            "Rainbow Six Siege",               "Multi-platform",    "Tactical Shooter",   "Precision-based tactical shooter with destructible environments and 60+ unique operators.",       449m,  41),
        new("repo.jpg",          "R.E.P.O.",                        "PC",                "Horror / Co-op",     "Collect valuable items from haunted locations and ship them out before the monsters get you.",    199m,  23),
        new("skyrim.jpg",        "The Elder Scrolls V: Skyrim",     "Multi-platform",    "RPG",                "The legendary open-world RPG. You are the Dragonborn — the fate of Skyrim is in your hands.",    349m,  89),
    ];

    public static async Task SeedAsync(IServiceProvider services, IWebHostEnvironment env)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blob = scope.ServiceProvider.GetRequiredService<BlobStorageService>();

        await db.Database.EnsureCreatedAsync();

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(new Role { Name = "Admin" }, new Role { Name = "Customer" });
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin");
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@gamestop.co.za",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = adminRole.Id
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Games.AnyAsync())
        {
            var seedDir = Path.Combine(env.WebRootPath, "seed-images");
            foreach (var g in Games)
            {
                var filePath = Path.Combine(seedDir, g.FileName);
                string imageUrl;
                if (File.Exists(filePath))
                {
                    var ext = Path.GetExtension(g.FileName).ToLowerInvariant();
                    var contentType = ext switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png"            => "image/png",
                        ".avif"           => "image/avif",
                        _                 => "application/octet-stream"
                    };
                    await using var stream = File.OpenRead(filePath);
                    imageUrl = await blob.UploadImageAsync(stream, g.FileName, contentType);
                }
                else
                {
                    imageUrl = "/images/placeholder.png";
                }

                db.Games.Add(new Game
                {
                    Title       = g.Title,
                    Platform    = g.Platform,
                    Genre       = g.Genre,
                    Description = g.Description,
                    Price       = g.Price,
                    StockCount  = g.Stock,
                    ImageUrl    = imageUrl,
                    CreatedAt   = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }
    }
}
