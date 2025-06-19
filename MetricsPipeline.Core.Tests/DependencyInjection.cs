using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace MetricsPipeline.Core.Tests;

public static class DependencyInjection
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ComparisonContext>();
        services.AddSingleton<IGoogleScanner>(sp => sp.GetRequiredService<ComparisonContext>().GoogleMock.Object);
        services.AddSingleton<IMicrosoftScanner>(sp => sp.GetRequiredService<ComparisonContext>().MicrosoftMock.Object);
        services.AddSingleton<MemoryStream>();
        services.AddSingleton<ILoggerFactory>(sp => LoggerFactory.Create(b => { }));
        return services;
    }
}
