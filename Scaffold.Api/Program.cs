using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

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
        return Task.CompletedTask;
    }
}