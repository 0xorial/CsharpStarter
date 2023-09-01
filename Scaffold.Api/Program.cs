using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Scaffold.Api;


// this structure is based on this example:
// https://github.com/dotnet/aspnetcore/issues/47255#issuecomment-1479482721

var builder = WebApplication.CreateBuilder(args);
await ConfigureBuilderAsync(builder);
var app = builder.Build();
await ConfigureApplicationAsync(app);

app.Run();

public partial class Program
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

        builder.Services.AddScoped<ITranslationService, TranslationService>();

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
            var translationService = context.RequestServices.GetRequiredService<ITranslationService>();
            var translation = await translationService.GetTranslation(rawRequestBody);
            var itemId = await connection.QuerySingleAsync<int>(
                $"INSERT INTO Items (Text) " +
                $"OUTPUT INSERTED.Id " +
                $"VALUES ('{rawRequestBody}')");
            await connection.ExecuteAsync(
                $"INSERT INTO Translations (ItemId, TranslatedText) VALUES ({itemId}, '{translation}')");
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
            await context.Response.WriteAsJsonAsync(items.Select(x => new ListedItemDto
            {
                Id = x.Id,
                Text = x.Text,
                TranslatedText = translationsByItemId[x.Id]?.TranslatedText
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