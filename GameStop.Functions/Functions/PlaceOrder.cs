using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GameStop.Functions.Functions;

public record OrderRequest(int GameId, int UserId, int Quantity);

public class PlaceOrder(ILogger<PlaceOrder> logger, IConfiguration config)
{
    [Function("PlaceOrder")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "PlaceOrder")] HttpRequestData req)
    {
        OrderRequest? order;
        try
        {
            order = await JsonSerializer.DeserializeAsync<OrderRequest>(req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON body");
            return bad;
        }

        if (order is null || order.GameId <= 0 || order.Quantity < 1)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("gameId and quantity (≥1) are required");
            return bad;
        }

        var orderId = Guid.NewGuid();
        var message = JsonSerializer.Serialize(new
        {
            orderId,
            order.GameId,
            order.UserId,
            order.Quantity,
            placedAt = DateTime.UtcNow
        });

        var connStr   = config["AzureWebJobsStorage"];
        var queueName = config["OrderQueueName"] ?? "orders-queue";
        var queueClient = new QueueClient(connStr, queueName);
        await queueClient.CreateIfNotExistsAsync();
        await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)));

        logger.LogInformation("Order {OrderId} queued for gameId={GameId}", orderId, order.GameId);

        var accepted = req.CreateResponse(HttpStatusCode.Accepted);
        accepted.Headers.Add("Content-Type", "application/json");
        await accepted.WriteStringAsync(JsonSerializer.Serialize(new { orderId, status = "Queued" }));
        return accepted;
    }
}
