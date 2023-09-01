using System.Data;
using System.Data.SqlClient;
using Dapper;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

// this structure is based on this example:
// https://github.com/dotnet/aspnetcore/issues/47255#issuecomment-1479482721

var builder = WebApplication.CreateBuilder(args);
await Scaffold.Api.Program.ConfigureBuilderAsync(builder);
var app = builder.Build();
await Scaffold.Api.Program.ConfigureApplicationAsync(app);

app.Run();

namespace Scaffold.Api
{
    public class Program
    {
        public static Task ConfigureBuilderAsync(WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.Console(theme: AnsiConsoleTheme.Code);
                })
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
                });

            builder.Services.AddScoped<IDbConnection>(services =>
                new SqlConnection(services.GetRequiredService<IConfiguration>().GetConnectionString("main")));

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddScoped<ITranslationService, TranslationService>();
            builder.Services.AddHostedService<TranslationFetchBackgroundService>();

            return Task.CompletedTask;
        }

        public static Task ConfigureApplicationAsync(WebApplication app)
        {
            app.MapGet("/", async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger>();
                logger.Information("sending hello");
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("Hello World!");
            });

            app.MapPost("/", async context =>
            {
                using var connection = context.RequestServices.GetRequiredService<IDbConnection>();
                connection.Open();
                var rawRequestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                await connection.ExecuteAsync(
                    $"INSERT INTO Items (Text) " +
                    $"VALUES ('{rawRequestBody}')");
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("OK!");
            });

            app.MapGet("/list", async context =>
            {
                using var connection = context.RequestServices.GetRequiredService<IDbConnection>();
                connection.Open();
                var items = (await connection.QueryAsync<ItemRecord>($"SELECT * FROM Items")).ToArray();
                var translations =
                    (await connection.QueryAsync<TranslationRecord>($"SELECT * FROM Translations")).ToArray();
                var translationsByItemId = translations.ToDictionary(x => x.ItemId);
                context.Response.StatusCode = 200;
                await context.Response.WriteAsJsonAsync(items.Select(x =>
                {
                    translationsByItemId.TryGetValue(x.Id, out var translationRecord);
                    return new ListedItemDto
                    {
                        Id = x.Id,
                        Text = x.Text,
                        TranslatedText = translationRecord?.TranslatedText
                    };
                }));
            });
            return Task.CompletedTask;
        }
    }

    public class TranslationRecord
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public required string TranslatedText { get; set; }
    }

    public class ItemRecord
    {
        public int Id { get; set; }
        public required string Text { get; set; }
    }

    public class ListedItemDto
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public required string? TranslatedText { get; set; }
    }
}