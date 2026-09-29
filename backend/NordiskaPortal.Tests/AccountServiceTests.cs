using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services;
using Xunit;

namespace NordiskaPortal.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IRepository<LedgerEntry>> _ledgerEntryRepoMock = new();
    private readonly Mock<IRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<ILogger<AccountService>> _loggerMock = new();
    private readonly ApplicationDbContext _dbContext;

    public AccountServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    private AccountService CreateService() =>
        new(
            _accountRepoMock.Object,
            _userRepoMock.Object,
            _transactionRepoMock.Object,
            _ledgerEntryRepoMock.Object,
            _notificationRepoMock.Object,
            _dbContext,
            _loggerMock.Object
        );

    [Fact]
    public async Task GetBalanceAsync_WhenAccountBelongsToAnotherUser_ShouldReturnNull()
    {
        var callerUserId = Guid.NewGuid();
        var actualOwnerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var foreignAccount = new Account
        {
            Id = accountId,
            UserId = actualOwnerId,
            AccountNumber = "NKM-88888",
            AccountType = "SAVINGS",
            Name = "Sparkonto"
        };

        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(foreignAccount);

        var service = CreateService();

        var result = await service.GetBalanceAsync(callerUserId, accountId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DepositAsync_WhenAmountIsZero_ShouldReturnFailure()
    {
        var service = CreateService();

        var result = await service.DepositAsync(Guid.NewGuid(), Guid.NewGuid(), 0m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be positive.");
    }

    [Fact]
    public async Task DepositAsync_WhenAmountIsNegative_ShouldReturnFailure()
    {
        var service = CreateService();

        var result = await service.DepositAsync(Guid.NewGuid(), Guid.NewGuid(), -50m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be positive.");
    }

    [Fact]
    public async Task WithdrawAsync_WhenAccountDoesNotExist_ShouldReturnFailure()
    {
        var accountId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync((Account?)null);

        var service = CreateService();

        var result = await service.WithdrawAsync(Guid.NewGuid(), accountId, 100m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account does not exist.");
    }
}