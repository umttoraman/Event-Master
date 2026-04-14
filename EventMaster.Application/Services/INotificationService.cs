namespace EventMaster.Application.Services;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendInAppAsync(Guid userId, string message, CancellationToken cancellationToken = default);
}
