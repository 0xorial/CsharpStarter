using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;
using System.Data;
using System.Data.SqlClient;
using Dapper;


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
            await connection.ExecuteAsync($"INSERT INTO Items (Text) VALUES ('{rawRequestBody}')");
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("OK!");
        });

        app.MapGet("/list", async context =>
        {
            using var connection = context.RequestServices.GetRequiredService<IDbConnection>();
            connection.Open();
            var items = (await connection.QueryAsync<Item>($"SELECT * FROM Items")).ToArray();
            context.Response.StatusCode = 200;
            await context.Response.WriteAsJsonAsync(items);
        });
        return Task.CompletedTask;
    }
}

public class Item
{
    public int Int { get; set; }
    public string Text { get; set; } = string.Empty;
}