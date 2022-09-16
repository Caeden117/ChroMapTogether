using ChroMapTogether.Configuration;
using ChroMapTogether.Models;
using ChroMapTogether.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ChroMapTogether.Filters;
using ChroMapTogether.Registries;
using System.Security.Cryptography;
using ChroMapTogether.Providers;
using System.Threading.Tasks;
using Serilog;
using Serilog.Exceptions.Core;

namespace ChroMapTogether
{
    public class Program
    {
        public static async Task Main(string[] args)
            => await CreateHostBuilder(args).Build().RunAsync().ConfigureAwait(false);

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder
                        .ConfigureServices((hostBuilderContext, services) =>
                            services
                                .AddOptions()
                                .AddConfiguration<ServerConfiguration>("ChroMapTogether")
                                .AddSingleton<ServerRegistry>()
                                .AddSingleton<ServerCodeProvider>()
                                .AddTransient<RNGCryptoServiceProvider>()
                                .AddControllers(options =>
                                    options.Filters.Add(new HttpResponseExceptionFilter())
                                )
                        )
                        .Configure(applicationBuilder =>
                            applicationBuilder
                                .UseHttpsRedirection()
                                .UseRouting()
                                .UseEndpoints(endPointRouteBuilder => endPointRouteBuilder.MapControllers())
                        )
                )
                .UseSerilog((host, services, logger) => logger
                    .ReadFrom.Configuration(host.Configuration)    
                    .Enrich.FromLogContext()
                    .WriteTo.Console());
    }
}
