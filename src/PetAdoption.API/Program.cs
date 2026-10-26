using System.Text.Json.Serialization;
using PetAdoption.API.Extensions;
using PetAdoption.API.Hateoas;
using PetAdoption.API.Middleware;
using PetAdoption.Application;
using PetAdoption.Infrastructure;
using PetAdoption.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LinkFactory>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPerformanceOptimizations();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    await DatabaseInitializer.InitializeAsync(app.Services);

app.UseExceptionHandler();
app.UseResponseCompression();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PetAdoption API v1");
    options.DocumentTitle = "PetAdoption API";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();
app.MapControllers();

app.Run();

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program { }
