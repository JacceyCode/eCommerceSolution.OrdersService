using BusinessLogicLayer.Mappers;
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

        return services;
    }
}
