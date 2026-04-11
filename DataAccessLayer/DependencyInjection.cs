using DataAccessLayer.Repository;
using DataAccessLayer.RepositoryContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace DataAccessLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration configuration) {
        string connectionString = configuration.GetConnectionString("MongoDB") ?? throw new InvalidOperationException("Connection string 'MongoDB' not found.");
        string databaseName = configuration["MongoDBDatabaseName"] ?? throw new InvalidOperationException("MongoDB database name not found in configuration.");

        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));

        services.AddScoped<IMongoDatabase>(provider =>
        {
            IMongoClient client = provider.GetRequiredService<IMongoClient>();

            //return client.GetDatabase("OrdersDatabase");
            return client.GetDatabase(databaseName);
        });

        services.AddScoped<IOrdersRepository, OrdersRepository>();

        return services;
    }
}
