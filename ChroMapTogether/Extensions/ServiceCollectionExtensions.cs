using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChroMapTogether.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfiguration<T>(
            this IServiceCollection services,
            string? sectionKey = null)
            where T : class, new() =>
            services.AddSingleton(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                if (!string.IsNullOrEmpty(sectionKey))
                    configuration = configuration.GetSection(sectionKey);
                var instance = configuration.Get<T>();
                if (instance is not null)
                    return instance;
                return new T();
            });
    }
}
