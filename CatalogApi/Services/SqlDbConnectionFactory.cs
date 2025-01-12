using CatalogApi.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CatalogApi.Services;
public class SqlDbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlDbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("MyCatalogDb");
        return new SqlConnection(connectionString);
    }
}

