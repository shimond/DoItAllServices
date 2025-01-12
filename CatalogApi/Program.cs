using CatalogApi.Contracts;
using CatalogApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddScoped<IDbConnectionFactory, SqlDbConnectionFactory>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/products", async (IProductRepository repository) =>
{
    var products  = await repository.GetAllProductsAsync();
    return Results.Ok(products);
});

app.MapGet("/api/products/search", async (string? name, decimal? price, IProductRepository repository) =>
{
    var products = await repository.SearchProductsAsync(name, price);
    return Results.Ok(products);
});

app.Run();
