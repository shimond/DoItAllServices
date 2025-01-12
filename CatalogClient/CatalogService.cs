using CatalogClient.Entities;
using System.Net.Http;
using System.Net.Http.Json;

namespace CatalogClient;

// ICatalogService.cs
public interface ICatalogService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> SearchProductsAsync(string? name, decimal? price);
}

// CatalogService.cs
public class CatalogService : ICatalogService
{
    private readonly HttpClient _httpClient;

    public CatalogService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("CatalogApi");
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Product>>("products");
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string? name, decimal? price)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query.Add($"name={Uri.EscapeDataString(name)}");
        }

        if (price.HasValue)
        {
            query.Add($"price={price.Value}");
        }

        var queryString = string.Join("&", query);
        var url = string.IsNullOrWhiteSpace(queryString) ? "products/search" : $"products/search?{queryString}";

        return await _httpClient.GetFromJsonAsync<IEnumerable<Product>>(url);
    }
}
