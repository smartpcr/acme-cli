using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Common.KeyVault;
using Common.Metrics;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
                    services.Configure<ServiceContext>(
                        hostBuilderContext.Configuration.GetSection(nameof(ServiceContext)));

                    // logging
                    services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddConfiguration(hostBuilderContext.Configuration.GetSection("Logging"));

                        loggingBuilder.AddConsole();
                    });

                    // key vault
                    services.Configure<KeyVaultSettings>(
                        hostBuilderContext.Configuration.GetSection(nameof(KeyVaultSettings)));
                    services.AddKeyVault();

                    // prometheus
                    if (hostBuilderContext.Configuration.UsePrometheus())
                    {
                        services.Configure<PrometheusSettings>(
                            hostBuilderContext.Configuration.GetSection(nameof(PrometheusSettings)));
                        services.AddPrometheus();
                    }

                    if (hostBuilderContext.Configuration.UseAppInsights())
                    {
                        services.Configure<AppInsightsSettings>(
                            hostBuilderContext.Configuration.GetSection(nameof(AppInsightsSettings)));
                        var serviceProvider = services.BuildServiceProvider();
                        var kvClient = serviceProvider.GetRequiredService<IKeyVaultClient>();
                        var kvSettings = serviceProvider.GetRequiredService<IOptions<KeyVaultSettings>>().Value;
                        var appInsightsSettings = serviceProvider.GetRequiredService<IOptions<AppInsightsSettings>>().Value;
                        var instrumentationKey = kvClient.GetSecretAsync(
                            kvSettings.VaultUrl,
                            appInsightsSettings.InstrumentationKeySecret).GetAwaiter().GetResult().Value;
                        appInsightsSettings.InstrumentationKey = instrumentationKey;
                        services.AddSingleton<IOptions<AppInsightsSettings>>(sp => new OptionsWrapper<AppInsightsSettings>(appInsightsSettings));

                        services.AddAppInsights();
                    }

                    RunAcmeRequestHandlerJob(hostBuilderContext, services, args)
                        .GetAwaiter().GetResult();
                });

            await builder.RunConsoleAsync();
        }

        private static async Task RunAcmeRequestHandlerJob(
            HostBuilderContext hostBuilderContext,
            IServiceCollection services,
            string[] args)
        {
            services.AddTransient<IAcmeContext, AcmeContext>();
            services.AddTransient<IDnsManagementClient, DnsManagementClient>();
            services.AddTransient<IResourceManagementClient, ResourceManagementClient>();
            services.AddTransient<IWebSiteManagementClient, WebSiteManagementClient>();

            var acme = new AcmeContext(WellKnownServers.LetsEncryptV2);
            var account = await acme.NewAccount("lingxd@gmail.com", true);
            var pemKey = acme.AccountKey.ToPem();

            var order = await acme.NewOrder(new[] {"*.dev.xiaodong.world"});
            var authz = (await order.Authorizations()).First();
            var dnsChallenge = await authz.Dns();
            var dnsText = acme.AccountKey.DnsTxt(dnsChallenge.Token);

            // TODO: add dns txt record to _acme-challenge.dev.xiaodong.world with dnsText value

            var httpChallenge = await authz.Http();
            var keyAuthz = httpChallenge.KeyAuthz;
        }
    }
}
