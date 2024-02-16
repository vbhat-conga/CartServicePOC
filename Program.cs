using CartServicePOC.DataModel;
using CartServicePOC.Extensions;
using CartServicePOC.Helper;
using CartServicePOC.Model;
using CartServicePOC.Service.Cart;
using CartServicePOC.Service.CartItem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    options.InvalidModelStateResponseFactory = c =>
    {
        var validationError = new ValidationProblemDetails(c.ModelState);
        return new BadRequestObjectResult(new ApiResponse<ValidationProblemDetails>(validationError, 400));
    })
    .AddJsonOptions(option =>
    {
        option.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        option.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        option.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<ICartService, CartService>();
builder.Services.AddTransient<ICartItemService, CartItemService>();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("cpqconnectionstring"), opt =>
    {
        opt.CommandTimeout(60);
    }));
var tracingExporter = builder.Configuration.GetValue("UseTracingExporter", defaultValue: "console")!.ToLowerInvariant();
var connection = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "127.0.0.1:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(connection);
// Build a resource configuration action to set service information.
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: builder.Configuration.GetValue("ServiceName", defaultValue: "cart-api")!,
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
    serviceInstanceId: Environment.MachineName);
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
// Configure OpenTelemetry tracing & metrics with auto-start using the
// AddOpenTelemetry extension from OpenTelemetry.Extensions.Hosting.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(appBuilder =>
    {
        // Tracing
        // Ensure the TracerProvider subscribes to any custom ActivitySources.
        appBuilder
            .AddSource(Instrumentation.ActivitySourceName)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRedisInstrumentation()
            .AddSqlClientInstrumentation();

        switch (tracingExporter)
        {
            case "otlp":
                appBuilder.AddOtlpExporter(otlpOptions =>
                {
                    // Use IConfiguration directly for Otlp exporter endpoint option.
                    otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://tempo-cpq:4317/api/traces")!);
                    otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                    otlpOptions.ExportProcessorType = ExportProcessorType.Batch;
                });
                break;

            default:
                break;
        }
    });
var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>();
app.ConfigureExceptionHandler(logger);
app.UseCorrelationId();
using var scope = app.Services.CreateScope();
await using var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
await dbContext.Database.MigrateAsync();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();

app.Run();
