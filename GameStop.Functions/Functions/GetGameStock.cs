using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace GameStop.Functions.Functions;

public class GetGameStock(ILogger<GetGameStock> logger, IConfiguration config)
{
    [Function("GetGameStock")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetGameStock")] HttpRequestData req)
    {
        if (!int.TryParse(req.Query["gameId"], out var gameId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("gameId query param required");
            return bad;
        }

        var connStr = config["SqlConnectionString"];
        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT Title, StockCount FROM Games WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", gameId);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Game not found");
                return notFound;
            }

            var title      = reader.GetString(0);
            var stockCount = reader.GetInt32(1);
            var status     = stockCount == 0 ? "OutOfStock" : stockCount <= 10 ? "LowStock" : "InStock";

            var ok = req.CreateResponse(HttpStatusCode.OK);
            ok.Headers.Add("Content-Type", "application/json");
            await ok.WriteStringAsync(JsonSerializer.Serialize(new { gameId, title, stockCount, status }));
            return ok;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetGameStock failed for gameId={GameId}", gameId);
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Internal error");
            return err;
        }
    }
}
