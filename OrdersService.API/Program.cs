using BusinessLogicLayer;
using DataAccessLayer;
using OrdersService.API.Middleware;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add BusinessLogicLayer and DataAccessLAyer
builder.Services.AddBusinessLogicLayer(builder.Configuration);
builder.Services.AddDataAccessLayer(builder.Configuration);

// Add controllers and other services to the IoC container
builder.Services.AddControllers();

// Add model binder to read values fro JSON to enum
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

// Add API explorer services
builder.Services.AddEndpointsApiExplorer();
// Add Swagger generation services
builder.Services.AddSwaggerGen();

// Add CORS policy to allow cross-origin requests from any origin, method, and header
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigns"] ?? "http://localhost:4200")
               .WithMethods("GET", "POST", "PUT", "DELETE")
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

// Use Global Exception Handling Middleware
app.UseGlobalExceptionHandlingMiddleware();

// Routing 
app.UseRouting();

// Add swagger middleware to serve generated Swagger as a JSON endpoint and the Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// Add CORS middleware to allow cross-origin requests from any origin, method, and header
app.UseCors();

// Auth
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();


app.UseDeveloperExceptionPage();


app.Run();
