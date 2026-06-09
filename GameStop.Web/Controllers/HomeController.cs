using GameStop.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStop.Web.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var featured = await db.Games
            .OrderByDescending(g => g.StockCount)
            .Take(4)
            .ToListAsync();
        return View(featured);
    }
}
