using Microsoft.Extensions.DependencyInjection;

namespace VectorSearch.Tests;

public abstract class TestBase: IAsyncLifetime
{
    protected readonly IServiceProvider ServiceProvider;

    protected TestBase()
    {
        var services = new ServiceCollection();
        services.RegisterServices();
        ServiceProvider = services.BuildServiceProvider();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual Task DisposeAsync() => Task.CompletedTask;
}