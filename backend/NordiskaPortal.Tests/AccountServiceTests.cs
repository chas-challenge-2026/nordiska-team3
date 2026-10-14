using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Models;
using NordiskaPortal.API.Repositories.Interfaces;
using NordiskaPortal.API.Services;
using Xunit;
using NordiskaPortal.API.Services.Interfaces;

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
    private readonly Mock<IAuditService> _auditMock = new();

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
            _auditMock.Object,
            _loggerMock.Object
        );

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();

    private Account OwnAccount(string name = "Sparkonto") => new()
    {
        Id = _accountId,
        UserId = _userId,
        AccountNumber = "NKM-12345",
        AccountType = "SAVINGS",
        Name = name
    };

    private Account ForeignAccount() => new()
    {
        Id = _accountId,
        UserId = Guid.NewGuid(),
        AccountNumber = "NKM-12345",
        AccountType = "SAVINGS",
        Name = "Sparkonto"
    };

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

    [Fact]
    public async Task GetUserTransactionsAsync_WhenAccountBelongsToAnotherUser_ShouldReturnNull()
    {
        var callerUserId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _accountRepoMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(new Account
        {
            Id = accountId,
            UserId = Guid.NewGuid(),
            AccountNumber = "NKM-88888",
            AccountType = "SAVINGS",
            Name = "Sparkonto"
        });

        var service = CreateService();

        var result = await service.GetUserTransactionsAsync(callerUserId, accountId, null, null, null, 1, 20);

        result.Should().BeNull();
        _transactionRepoMock.Verify(r => r.GetByUserIdAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_ShouldTreatDatesAsInclusiveUtcDays()
    {
        var userId = Guid.NewGuid();

        _transactionRepoMock
            .Setup(r => r.GetByUserIdAsync(userId, null, null,
                new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), 1, 20))
            .ReturnsAsync(((IReadOnlyList<Transaction>)new List<Transaction>(), 0));

        var service = CreateService();

        var result = await service.GetUserTransactionsAsync(
            userId, null, null, new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 7), 1, 20);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_ShouldIncludeAccountIdAndTotalPages()
    {
        var userId = Guid.NewGuid();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            new() { AccountId = accountA, Amount = 100m, TransactionType = "DEPOSIT", Status = "COMPLETED" },
            new() { AccountId = accountB, Amount = 50m, TransactionType = "WITHDRAWAL", Status = "COMPLETED" }
        };

        _transactionRepoMock
            .Setup(r => r.GetByUserIdAsync(userId, null, null, null, null, 1, 20))
            .ReturnsAsync(((IReadOnlyList<Transaction>)transactions, 45));

        var service = CreateService();

        var result = await service.GetUserTransactionsAsync(userId, null, null, null, null, 1, 20);

        result.Should().NotBeNull();
        result!.TotalPages.Should().Be(3);
        result.Transactions.Select(t => t.AccountId).Should().Equal(accountA, accountB);
    }

    [Theory]
    [InlineData("savings", "SAVINGS", "Sparkonto")]
    [InlineData("Checking", "CHECKING", "Transaktionskonto")]
    [InlineData("SAVINGS", "SAVINGS", "Sparkonto")]
    public async Task CreateAccountAsync_SavesTheAccountTypeInUpperCase(string requested, string expectedType, string expectedName)
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId });
        _accountRepoMock.Setup(r => r.ExistsByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync(false);
        Account? saved = null;
        _accountRepoMock.Setup(r => r.AddAsync(It.IsAny<Account>())).Callback<Account>(a => saved = a);

        var result = await CreateService().CreateAccountAsync(userId, requested);

        saved.Should().NotBeNull();
        saved!.AccountType.Should().Be(expectedType);
        saved.Name.Should().Be(expectedName);
        result!.AccountType.Should().Be(expectedType);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenTheUserDoesNotExist_ReturnsNullAndSavesNothing()
    {
        var result = await CreateService().CreateAccountAsync(_userId, "SAVINGS");

        result.Should().BeNull();
        _accountRepoMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task CreateAccountAsync_ForATypeWithoutADefaultName_UsesTheTypeAsName()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(new User { Id = _userId });
        Account? saved = null;
        _accountRepoMock.Setup(r => r.AddAsync(It.IsAny<Account>())).Callback<Account>(a => saved = a);

        var result = await CreateService().CreateAccountAsync(_userId, "other");

        saved!.Name.Should().Be("OTHER");
        result!.Balance.Should().Be("0.00");
        result.AccountNumber.Should().StartWith("NKM-");
        _accountRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenNoFreeAccountNumberIsFound_Throws()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(new User { Id = _userId });
        _accountRepoMock.Setup(r => r.ExistsByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync(true);

        var act = () => CreateService().CreateAccountAsync(_userId, "SAVINGS");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _accountRepoMock.Verify(r => r.ExistsByAccountNumberAsync(It.IsAny<string>()), Times.Exactly(5));
    }

    [Fact]
    public async Task RenameAccountAsync_ByTheOwner_SavesTheNewNameAndReturnsTheBalance()
    {
        var account = OwnAccount();
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(account);
        _accountRepoMock.Setup(r => r.GetBalanceAsync(_accountId)).ReturnsAsync(150.5m);

        var result = await CreateService().RenameAccountAsync(_userId, _accountId, "Semesterkassan");

        account.Name.Should().Be("Semesterkassan");
        result!.Name.Should().Be("Semesterkassan");
        result.Balance.Should().Be("150.50");
        _accountRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RenameAccountAsync_ForAnotherUsersAccount_ReturnsNullAndSavesNothing()
    {
        var account = ForeignAccount();
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(account);

        var result = await CreateService().RenameAccountAsync(_userId, _accountId, "Mitt konto");

        result.Should().BeNull();
        account.Name.Should().Be("Sparkonto");
        _accountRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RenameAccountAsync_WhenTheAccountDoesNotExist_ReturnsNull()
    {
        var result = await CreateService().RenameAccountAsync(_userId, _accountId, "Mitt konto");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountsForUserAsync_ReturnsEveryAccountWithItsBalance()
    {
        var second = new Account { Id = Guid.NewGuid(), UserId = _userId, AccountNumber = "NKM-54321", AccountType = "CHECKING", Name = "Transaktionskonto" };
        _accountRepoMock.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync((IReadOnlyList<Account>)new List<Account> { OwnAccount(), second });
        _accountRepoMock.Setup(r => r.GetBalanceAsync(_accountId)).ReturnsAsync(1000m);
        _accountRepoMock.Setup(r => r.GetBalanceAsync(second.Id)).ReturnsAsync(0.5m);

        var result = await CreateService().GetAccountsForUserAsync(_userId);

        result.Accounts.Select(a => a.AccountNumber).Should().Equal("NKM-12345", "NKM-54321");
        result.Accounts.Select(a => a.Balance).Should().Equal("1000.00", "0.50");
    }

    [Fact]
    public async Task GetAccountsForUserAsync_WhenTheUserHasNoAccounts_ReturnsAnEmptyList()
    {
        _accountRepoMock.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync((IReadOnlyList<Account>)new List<Account>());

        var result = await CreateService().GetAccountsForUserAsync(_userId);

        result.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBalanceAsync_ForTheOwner_ReturnsTheBalanceWithTwoDecimals()
    {
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(OwnAccount());
        _accountRepoMock.Setup(r => r.GetBalanceAsync(_accountId)).ReturnsAsync(12m);

        var result = await CreateService().GetBalanceAsync(_userId, _accountId);

        result!.Balance.Should().Be("12.00");
        result.Id.Should().Be(_accountId);
    }

    [Fact]
    public async Task DepositAsync_WhenTheAccountDoesNotExist_ReturnsFailure()
    {
        var result = await CreateService().DepositAsync(_userId, _accountId, 100m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account does not exist.");
    }

    [Fact]
    public async Task DepositAsync_ForAnotherUsersAccount_ReturnsFailure()
    {
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(ForeignAccount());

        var result = await CreateService().DepositAsync(_userId, _accountId, 100m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account does not exist.");
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task WithdrawAsync_WhenTheAmountIsNotPositive_ReturnsFailure(int amount)
    {
        var result = await CreateService().WithdrawAsync(_userId, _accountId, amount);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Amount must be positive.");
    }

    [Fact]
    public async Task WithdrawAsync_ForAnotherUsersAccount_ReturnsFailure()
    {
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(ForeignAccount());

        var result = await CreateService().WithdrawAsync(_userId, _accountId, 100m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account does not exist.");
    }

    [Fact]
    public async Task TransferAsync_WhenTheSignedInUserCannotBeFound_ReturnsFailure()
    {
        var recipient = new Account { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), AccountNumber = "NKM-99999", AccountType = "CHECKING", Name = "Transaktionskonto" };
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(OwnAccount());
        _accountRepoMock.Setup(r => r.GetByAccountNumberAsync("NKM-99999")).ReturnsAsync(recipient);

        var result = await CreateService().TransferAsync(_userId, _accountId, "NKM-99999", 100m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account does not exist.");
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_ForTheOwner_ReturnsThePageWithItsTotals()
    {
        var transfer = Guid.NewGuid();
        var items = new List<Transaction>
        {
            new() { AccountId = _accountId, Amount = 12.5m, TransactionType = "TRANSFER_OUT", Status = "COMPLETED", TransferId = transfer, CounterpartyLabel = "Anna A." }
        };
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(OwnAccount());
        _transactionRepoMock.Setup(r => r.GetByAccountIdAsync(_accountId, 2, 10)).ReturnsAsync(((IReadOnlyList<Transaction>)items, 21));

        var result = await CreateService().GetTransactionHistoryAsync(_userId, _accountId, 2, 10);

        result!.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(21);
        result.TotalPages.Should().Be(3);
        var dto = result.Transactions.Single();
        dto.Amount.Should().Be("12.50");
        dto.TransactionType.Should().Be("TRANSFER_OUT");
        dto.TransferId.Should().Be(transfer);
        dto.Counterparty.Should().Be("Anna A.");
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_ForAnotherUsersAccount_ReturnsNull()
    {
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(ForeignAccount());

        var result = await CreateService().GetTransactionHistoryAsync(_userId, _accountId, 1, 10);

        result.Should().BeNull();
        _transactionRepoMock.Verify(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LookupRecipientAsync_WhenTheOwnerCannotBeFound_ReturnsNull()
    {
        _accountRepoMock.Setup(r => r.GetByAccountNumberAsync("NKM-12345")).ReturnsAsync(OwnAccount());

        var result = await CreateService().LookupRecipientAsync("NKM-12345");

        result.Should().BeNull();
    }

    [Fact]
    public async Task LookupRecipientAsync_WhenTheOwnerHasNoLastName_ReturnsOnlyTheFirstName()
    {
        _accountRepoMock.Setup(r => r.GetByAccountNumberAsync("NKM-12345")).ReturnsAsync(OwnAccount());
        _userRepoMock.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(new User { Id = _userId, FirstName = " Robin ", LastName = " " });

        var result = await CreateService().LookupRecipientAsync("NKM-12345");

        result!.OwnerName.Should().Be("Robin");
    }

    [Fact]
    public async Task GetUserTransactionsAsync_WithTheLatestPossibleToDate_HasNoUpperLimit()
    {
        _transactionRepoMock
            .Setup(r => r.GetByUserIdAsync(_userId, null, null, null, null, 1, 20))
            .ReturnsAsync(((IReadOnlyList<Transaction>)new List<Transaction>(), 0));

        var result = await CreateService().GetUserTransactionsAsync(_userId, null, null, null, DateOnly.MaxValue, 1, 20);

        result!.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetUserTransactionsAsync_ForAnOwnAccount_PassesTheFiltersOn()
    {
        _accountRepoMock.Setup(r => r.GetByIdAsync(_accountId)).ReturnsAsync(OwnAccount());
        _transactionRepoMock
            .Setup(r => r.GetByUserIdAsync(_userId, _accountId, "DEPOSIT", null, null, 1, 20))
            .ReturnsAsync(((IReadOnlyList<Transaction>)new List<Transaction>(), 0));

        var result = await CreateService().GetUserTransactionsAsync(_userId, _accountId, "DEPOSIT", null, null, 1, 20);

        result.Should().NotBeNull();
        _transactionRepoMock.Verify(r => r.GetByUserIdAsync(_userId, _accountId, "DEPOSIT", null, null, 1, 20), Times.Once);
    }
}