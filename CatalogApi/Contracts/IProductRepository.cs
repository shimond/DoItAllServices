using CatalogApi.Entities;

namespace CatalogApi.Contracts;
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> SearchProductsAsync(string? name, decimal? price);
}