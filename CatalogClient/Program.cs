
// ConsoleClient.cs
using System.Net.Http.Json;
using CatalogClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("CatalogApi", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7135/api/"); // Adjust URL as needed
        });
        services.AddScoped<ICatalogService, CatalogService>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

while (true)
{
    Console.WriteLine("Choose an option:");
    Console.WriteLine("1. Get all products");
    Console.WriteLine("2. Search products");
    Console.WriteLine("3. Exit");
    Console.Write("Enter your choice: ");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            var products = await catalogService.GetAllProductsAsync();
            Console.WriteLine("\nProducts:");
            foreach (var product in products)
            {
                Console.WriteLine($"Id: {product.Id}, Name: {product.Name}, Price: {product.Price:C}");
            }
            Console.WriteLine();
            break;

        case "2":
            Console.Write("Enter product name (leave blank to skip): ");
            var name = Console.ReadLine();

            Console.Write("Enter product price (leave blank to skip): ");
            var priceInput = Console.ReadLine();
            decimal? price = string.IsNullOrWhiteSpace(priceInput) ? null : decimal.Parse(priceInput);

            var searchResults = await catalogService.SearchProductsAsync(name, price);
            Console.WriteLine("\nSearch Results:");
            foreach (var product in searchResults)
            {
                Console.WriteLine($"Id: {product.Id}, Name: {product.Name}, Price: {product.Price:C}");
            }
            Console.WriteLine();
            break;

        case "3":
            Console.WriteLine("Exiting...");
            return;

        default:
            Console.WriteLine("Invalid choice. Please try again.\n");
            break;
    }
}
