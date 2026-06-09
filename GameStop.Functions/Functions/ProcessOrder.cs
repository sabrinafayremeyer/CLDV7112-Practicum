using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GameStop.Functions.Functions;

public class ProcessOrder(ILogger<ProcessOrder> logger, IConfiguration config)
{
    [Function("ProcessOrder")]
    public async Task Run(
        [QueueTrigger("orders-queue", Connection = "AzureWebJobsStorage")] string messageBase64)
    {
        string json;
        try { json = Encoding.UTF8.GetString(Convert.FromBase64String(messageBase64)); }
        catch { json = messageBase64; }

        logger.LogInformation("Processing order message: {Message}", json);

        using var doc = JsonDocument.Parse(json);
        var root     = doc.RootElement;
        var orderId  = root.GetProperty("orderId").GetString() ?? Guid.NewGuid().ToString();
        var gameId   = root.GetProperty("gameId").GetInt32();
        var quantity = root.GetProperty("quantity").GetInt32();

        var connStr   = config["SqlConnectionString"];
        var tableConn = config["AzureWebJobsStorage"];
        var tableName = config["AuditTableName"] ?? "AuditLogs";

        string status;
        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(
                "UPDATE Games SET StockCount = StockCount - @qty WHERE Id = @gameId AND StockCount >= @qty", conn);
            cmd.Parameters.AddWithValue("@qty", quantity);
            cmd.Parameters.AddWithValue("@gameId", gameId);
            var rows = await cmd.ExecuteNonQueryAsync();

            status = rows > 0 ? "Fulfilled" : "InsufficientStock";
            logger.LogInformation("Order {OrderId}: {Status}", orderId, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update stock for order {OrderId}", orderId);
            status = "Error";
        }

        // write audit log
        try
        {
            var tableClient = new TableClient(tableConn, tableName);
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(new TableEntity("Order", Guid.NewGuid().ToString())
            {
                ["OrderId"]     = orderId,
                ["GameId"]      = gameId,
                ["Quantity"]    = quantity,
                ["Status"]      = status,
                ["ProcessedAt"] = DateTime.UtcNow.ToString("O")
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for order {OrderId}", orderId);
        }
    }
}
