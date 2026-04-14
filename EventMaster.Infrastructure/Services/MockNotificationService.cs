using EventMaster.Application.Services;
using Microsoft.Extensions.Logging;

namespace EventMaster.Infrastructure.Services;

public class MockNotificationService : INotificationService
{
    private readonly ILogger<MockNotificationService> _logger;

    public MockNotificationService(ILogger<MockNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[EMAIL MOCK] To={To} Subject={Subject} Body={Body}", to, subject, body);
        return Task.CompletedTask;
    }

    public Task SendInAppAsync(Guid userId, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[IN-APP MOCK] User={UserId} Message={Message}", userId, message);
        return Task.CompletedTask;
    }
}
