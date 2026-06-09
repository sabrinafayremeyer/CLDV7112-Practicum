using Azure.Storage.Queues;
using GameStop.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace GameStop.Web.Controllers;

public class OrdersController(FunctionService functions, IConfiguration config) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Buy(int gameId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return Json(new { success = false, message = "Please log in to purchase." });

        // try via Function first
        var ok = await functions.PlaceOrderAsync(gameId, userId.Value);

        if (!ok)
        {
            // fallback: enqueue directly to storage queue
            try
            {
                var connStr   = config["AzureStorage:ConnectionString"];
                var queueName = config["AzureStorage:OrderQueue"] ?? "orders-queue";
                var client    = new QueueClient(connStr, queueName);
                await client.CreateIfNotExistsAsync();

                var message = JsonSerializer.Serialize(new
                {
                    orderId  = Guid.NewGuid(),
                    gameId,
                    userId   = userId.Value,
                    quantity = 1,
                    placedAt = DateTime.UtcNow
                });
                await client.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)));
                ok = true;
            }
            catch { /* fallback also failed */ }
        }

        return Json(new
        {
            success = ok,
            message = ok ? "Added to cart! Your order is being processed." : "Could not place order. Please try again."
        });
    }
}
