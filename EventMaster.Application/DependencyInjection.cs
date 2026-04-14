using EventMaster.Application.Services;
using EventMaster.Application.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace EventMaster.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
