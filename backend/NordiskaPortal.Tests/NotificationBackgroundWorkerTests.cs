using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Services;
using NordiskaPortal.API.Services.Interfaces;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class NotificationBackgroundWorkerTests : IDisposable
{
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly ServiceProvider _provider;

    public NotificationBackgroundWorkerTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton(_emailSender.Object);
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task ProcessPendingAsync_SendsThePendingNotificationAndMarksItSent()
    {
        var id = await AddNotificationAsync();

        await CreateWorker().ProcessPendingAsync(CancellationToken.None);

        var notification = await GetNotificationAsync(id);
        notification.Status.Should().Be("SENT");
        notification.SentAt.Should().NotBeNull();
        _emailSender.Verify(s => s.SendAsync("anna@example.com", "DEPOSIT", "hello"), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenSendingFails_CountsTheAttemptAndKeepsTheNotificationPending()
    {
        SetupSenderToFail();
        var id = await AddNotificationAsync();

        await CreateWorker().ProcessPendingAsync(CancellationToken.None);

        var notification = await GetNotificationAsync(id);
        notification.Status.Should().Be("PENDING");
        notification.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenTheLastAttemptFails_MarksTheNotificationFailed()
    {
        SetupSenderToFail();
        var id = await AddNotificationAsync(retryCount: 2, createdAt: DateTime.UtcNow.AddHours(-1));

        await CreateWorker().ProcessPendingAsync(CancellationToken.None);

        var notification = await GetNotificationAsync(id);
        notification.Status.Should().Be("FAILED");
        notification.RetryCount.Should().Be(3);
    }

    [Fact]
    public async Task ProcessPendingAsync_WaitsBeforeRetryingAFailedNotification()
    {
        var id = await AddNotificationAsync(retryCount: 1, createdAt: DateTime.UtcNow);

        await CreateWorker().ProcessPendingAsync(CancellationToken.None);

        _emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        var notification = await GetNotificationAsync(id);
        notification.Status.Should().Be("PENDING");
        notification.RetryCount.Should().Be(1);
    }

    private NotificationBackgroundWorker CreateWorker() =>
        new(_provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<NotificationBackgroundWorker>.Instance);

    private void SetupSenderToFail() =>
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("mail server is down"));

    private async Task<Guid> AddNotificationAsync(int retryCount = 0, DateTime? createdAt = null)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            PersonalNumber = "19950101-0001",
            FirstName = "Anna",
            LastName = "Andersson",
            Email = "anna@example.com",
            PinHash = "hash"
        };

        var notification = new Notification
        {
            User = user,
            Type = "DEPOSIT",
            Message = "hello",
            RetryCount = retryCount,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification.Id;
    }

    private async Task<Notification> GetNotificationAsync(Guid id)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Notifications.AsNoTracking().SingleAsync(n => n.Id == id);
    }
}