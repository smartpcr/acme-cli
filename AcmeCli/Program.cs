using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AcmeCli
{
    class Program
    {
        static Program()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                {
                    configurationBuilder.AddJsonFile("appsettings.json");
                })
                .ConfigureServices((hostBuilderContext, services) =>
                {
                    services.AddOptions();
                    services.Configure<KeyVaultSettings>(
                        hostBuilderContext.Configuration.GetSection(nameof(KeyVaultSettings)));
                    services.Configure<ServiceContext>(
                        hostBuilderContext.Configuration.GetSection(nameof(ServiceContext)));
                    services.Configure<AppInsightsSettings>(
                        hostBuilderContext.Configuration.GetSection(nameof(AppInsightsSettings)));
                    services.Configure<PrometheusSettings>(
                        hostBuilderContext.Configuration.GetSection(nameof(PrometheusSettings)));
                });
            await builder.RunConsoleAsync();
        }
    }
}
