using CatalogApi.Contracts;
using CatalogApi.Entities;
using Dapper;
using System.Text;

namespace CatalogApi.Services;
public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string query = "SELECT Id, Name, Price FROM Products";
        return await connection.QueryAsync<Product>(query);
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string? name, decimal? price)
    {
        using var connection = _connectionFactory.CreateConnection();
        var query = new StringBuilder("SELECT Id, Name, Price FROM Products WHERE 1 = 1");
        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query.Append(" AND Name LIKE @Name");
            parameters.Add("@Name", $"%{name}%");
        }
        if (price.HasValue)
        {
            query.Append(" AND Price = @Price");
            parameters.Add("@Price", price);
        }
        return await connection.QueryAsync<Product>(query.ToString(), parameters);
    }
}