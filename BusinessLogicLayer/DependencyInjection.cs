using BusinessLogicLayer.Mappers;
using BusinessLogicLayer.Policies;
using BusinessLogicLayer.RabbitMQ;
using BusinessLogicLayer.ServiceContracts;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services, IConfiguration configuration)
    {
        // AutoMapper
        services.AddAutoMapper(cfg => { }, typeof(OrderAddRequestToOrderMappingProfile).Assembly);


        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<OrderAddRequestValidator>();

        services.AddScoped<IOrdersService, OrdersService>();


        // Add service policy as a transcient service
        services.AddTransient<IUsersMicroservicePolicies, UsersMicroservicePolicies>();
        services.AddTransient<IProductsMicroservicePolicies, ProductsMicroservicePolicies>();
        services.AddTransient<IPollyPolicies, PollyPolicies>();

        // Redis cache configuration
        services.AddStackExchangeRedisCache(options =>
        {
            string redisHost = configuration["Redis_Host"] ?? "localhost";
            string redisPort = configuration["Redis_Port"] ?? "6379";


            options.Configuration = $"{redisHost}:{redisPort}";
        });

        // Add consumer class
        services.AddTransient<IRabbitMQConsumer, RabbitMQConsumer>();

        // Add hosted service
        services.AddHostedService<RabbitMQProductNameUpdateHostedService>();

        // Add heath checks for dependent services
        services.AddHealthChecks()
            .AddRedis(options =>
        {
            string redisHost = configuration["Redis_Host"] ?? "localhost";
            string redisPort = configuration["Redis_Port"] ?? "6379";

            return $"{redisHost}:{redisPort}";
        });

        return services;
    }
}
