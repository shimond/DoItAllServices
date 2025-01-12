using System.Data;

namespace CatalogApi.Contracts;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

