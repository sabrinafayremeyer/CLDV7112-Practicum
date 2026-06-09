using System.Text;
using System.Text.Json;

namespace GameStop.Web.Services;

public class StockResult
{
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int StockCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class FunctionService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _getStockPath;
    private readonly string _placeOrderPath;

    public FunctionService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _baseUrl = config["AzureFunctions:BaseUrl"] ?? string.Empty;
        _getStockPath = config["AzureFunctions:GetStockPath"] ?? "/api/GetGameStock";
        _placeOrderPath = config["AzureFunctions:PlaceOrderPath"] ?? "/api/PlaceOrder";
    }

    public async Task<StockResult?> GetGameStockAsync(int gameId)
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}{_getStockPath}?gameId={gameId}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<StockResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    public async Task<bool> PlaceOrderAsync(int gameId, int userId, int quantity = 1)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { gameId, userId, quantity });
            var response = await _http.PostAsync($"{_baseUrl}{_placeOrderPath}",
                new StringContent(body, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
