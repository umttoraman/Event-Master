using EventMaster.Application.Abstractions;
using EventMaster.Application.Services;
using EventMaster.Infrastructure.Options;
using EventMaster.Infrastructure.Persistence;
using EventMaster.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventMaster.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DocumentStorageOptions>(configuration.GetSection(DocumentStorageOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<INotificationService, MockNotificationService>();
        services.AddScoped<IDocumentService, DocumentService>();

        return services;
    }
}
