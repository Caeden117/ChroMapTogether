using ChroMapTogether.Configuration;
using ChroMapTogether.Models;
using ChroMapTogether.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChroMapTogether.Filters;
using ChroMapTogether.Registries;
using System.Security.Cryptography;
using ChroMapTogether.Providers;
using System.Threading.Tasks;
using Serilog;
using Serilog.Exceptions.Core;
using ChroMapTogether.UDP;
using Microsoft.AspNetCore.HttpOverrides;

namespace ChroMapTogether
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            host.Services.GetRequiredService<UDPServer>();

            await host.RunAsync().ConfigureAwait(false);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder
                        .ConfigureServices((hostBuilderContext, services) =>
                            services
                                .AddOptions()
                                .AddConfiguration<ServerConfiguration>("ChroMapTogether")
                                .AddSingleton<SessionRegistry>()
                                .AddSingleton<SessionCodeProvider>()
                                .AddTransient<RNGCryptoServiceProvider>()
                                .AddSingleton<UDPServer>()
                                .AddControllers(options =>
                                    options.Filters.Add(new HttpResponseExceptionFilter())
                                )
                        )
                        .Configure(applicationBuilder => {
                            var forwardedHeadersOptions = new ForwardedHeadersOptions()
                            {
                                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                            };

                            forwardedHeadersOptions.KnownNetworks.Clear();
                            forwardedHeadersOptions.KnownProxies.Clear();

                            applicationBuilder
                                .UseHttpsRedirection()
                                .UseForwardedHeaders(forwardedHeadersOptions)
                                .UseRouting()
                                .UseEndpoints(endPointRouteBuilder => endPointRouteBuilder.MapControllers());
                        })
                )
                .UseSerilog((host, services, logger) => logger
                    .ReadFrom.Configuration(host.Configuration)    
                    .Enrich.FromLogContext()
                    .MinimumLevel.Information()
                    .WriteTo.Console());
    }
}
