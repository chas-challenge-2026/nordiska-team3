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
    private readonly Mock<IRepository<Transaction>> _transactionRepoMock = new();
    private readonly Mock<IRepository<LedgerEntry>> _ledgerEntryRepoMock = new();
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
            _dbContext,
            _loggerMock.Object
        );

    [Fact]
    public async Task GetAccountsForUserAsync_ShouldReturnAccountsWithCorrectBalance_ForGivenUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new()
            {
                Id = accountId,
                UserId = userId,
                AccountNumber = "NKM-12345",
                AccountType = "CHECKING",
                Status = "ACTIVE"
            }
        };

        _accountRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(accounts);
        _accountRepoMock.Setup(r => r.GetBalanceAsync(accountId)).ReturnsAsync(2500.75m);

        var service = CreateService();

        // Act
        var result = await service.GetAccountsForUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Accounts.Should().HaveCount(1);
        result.Accounts[0].AccountNumber.Should().Be("NKM-12345");
        result.Accounts[0].Balance.Should().Be("2500.75");
    }

    [Fact]
    public async Task GetBalanceAsync_WhenAccountBelongsToAnotherUser_ShouldReturnNull()
    {
        // Arrange: 
        var callerUserId = Guid.NewGuid();
        var actualOwnerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var foreignAccount = new Account
        {
            Id = accountId,
            UserId = actualOwnerId,
            AccountNumber = "NKM-88888",
            AccountType = "SAVINGS"
        };

        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(foreignAccount);

        var service = CreateService();

        // Act
        var result = await service.GetBalanceAsync(callerUserId, accountId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DepositAsync_WhenAmountIsZeroOrNegative_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var service = CreateService();

        // Act
        var result = await service.DepositAsync(userId, accountId, -50m);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be positive.");
    }
}