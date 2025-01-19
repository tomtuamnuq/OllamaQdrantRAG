using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VectorSearch;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) => { services.RegisterServices(); })
            .Build();

        var app = host.Services.GetRequiredService<App>();
        await app.RunAsync(args);
    }
}