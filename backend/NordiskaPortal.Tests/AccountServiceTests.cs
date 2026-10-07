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

    [Fact]
    public async Task TransferAsync_WhenAmountIsNotPositive_ShouldReturnFailure()
    {
        var service = CreateService();

        var result = await service.TransferAsync(Guid.NewGuid(), Guid.NewGuid(), "NKM-22222", 0m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be positive.");
    }

    [Fact]
    public async Task TransferAsync_WhenSourceAccountBelongsToAnotherUser_ShouldReturnFailure()
    {
        var fromId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByIdAsync(fromId)).ReturnsAsync(
            new Account { Id = fromId, UserId = Guid.NewGuid(), AccountNumber = "NKM-11111", AccountType = "SAVINGS", Name = "Sparkonto" });

        var service = CreateService();

        var result = await service.TransferAsync(Guid.NewGuid(), fromId, "NKM-22222", 10m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account does not exist.");
    }

    [Fact]
    public async Task TransferAsync_WhenRecipientDoesNotExist_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByIdAsync(fromId)).ReturnsAsync(
            new Account { Id = fromId, UserId = userId, AccountNumber = "NKM-11111", AccountType = "SAVINGS", Name = "Sparkonto" });
        _accountRepoMock.Setup(r => r.GetByAccountNumberAsync("NKM-99999")).ReturnsAsync((Account?)null);

        var service = CreateService();

        var result = await service.TransferAsync(userId, fromId, "NKM-99999", 10m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Recipient account does not exist.");
    }

    [Fact]
    public async Task TransferAsync_WhenSameAccount_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = new Account { Id = accountId, UserId = userId, AccountNumber = "NKM-11111", AccountType = "SAVINGS", Name = "Sparkonto" };
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
        _accountRepoMock.Setup(r => r.GetByAccountNumberAsync("NKM-11111")).ReturnsAsync(account);

        var service = CreateService();

        var result = await service.TransferAsync(userId, accountId, "NKM-11111", 10m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Accounts must be different.");
    }

    [Fact]
    public async Task LookupRecipientAsync_WhenAccountDoesNotExist_ShouldReturnNull()
    {
        _accountRepoMock.Setup(r => r.GetByAccountNumberAsync("NKM-99999")).ReturnsAsync((Account?)null);

        var service = CreateService();

        var result = await service.LookupRecipientAsync("NKM-99999");

        result.Should().BeNull();
    }

    [Fact]
    public async Task LookupRecipientAsync_ShouldMaskLastName()
    {
        var ownerId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByAccountNumberAsync("NKM-22222")).ReturnsAsync(
            new Account { Id = Guid.NewGuid(), UserId = ownerId, AccountNumber = "NKM-22222", AccountType = "SAVINGS", Name = "Sparkonto" });
        _userRepoMock.Setup(r => r.GetByIdAsync(ownerId)).ReturnsAsync(
            new User { Id = ownerId, FirstName = "Anna", LastName = "Lindberg", Email = "anna@example.com", PersonalNumber = "x", PinHash = "x" });

        var service = CreateService();

        var result = await service.LookupRecipientAsync("NKM-22222");

        result.Should().NotBeNull();
        result!.OwnerName.Should().Be("Anna L.");
        result.AccountNumber.Should().Be("NKM-22222");
    }
}