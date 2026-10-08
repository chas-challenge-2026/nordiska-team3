using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NordiskaPortal.API.Data;
using NordiskaPortal.API.Services;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace NordiskaPortal.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private static readonly byte[] Key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
    private readonly string _workDirectory = Path.Combine(Path.GetTempPath(), "audit-key-tests-" + Guid.NewGuid());
    private readonly ApplicationDbContext _context;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
    }

    private string KeyFilePath => Path.Combine(_workDirectory, "audit.key");

    [Fact]
    public async Task AppendAsync_StoresWhoDidWhatToWhich()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        await AppendAndSaveAsync(CreateService(), userId, "DEPOSIT", entityId, new { amount = "100.00" });

        var entry = await _context.AuditEntries.SingleAsync();
        entry.UserId.Should().Be(userId);
        entry.Action.Should().Be("DEPOSIT");
        entry.EntityType.Should().Be("Transaction");
        entry.EntityId.Should().Be(entityId);
        entry.Details.Should().Be("{\"amount\":\"100.00\"}");
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AppendAsync_FirstEntryStartsTheChainFromTheGenesisHash()
    {
        await AppendAndSaveAsync(CreateService());

        var entry = await _context.AuditEntries.SingleAsync();
        entry.PreviousHash.Should().Be(AuditChain.GenesisHash);
        entry.Hash.Should().HaveLength(64).And.NotBe(AuditChain.GenesisHash);
    }

    [Fact]
    public async Task AppendAsync_LinksEachEntryToTheHashOfTheOneBefore()
    {
        var service = CreateService();
        await AppendAndSaveAsync(service);
        await AppendAndSaveAsync(service);
        await AppendAndSaveAsync(service);

        var entries = await _context.AuditEntries.OrderBy(a => a.Id).ToListAsync();
        entries[1].PreviousHash.Should().Be(entries[0].Hash);
        entries[2].PreviousHash.Should().Be(entries[1].Hash);
    }

    [Fact]
    public async Task AppendAsync_WhenAnotherEntryIsStillUnsaved_Throws()
    {
        var service = CreateService();
        await service.AppendAsync(null, "DEPOSIT", "Transaction", Guid.NewGuid(), new { });

        var act = () => service.AppendAsync(null, "DEPOSIT", "Transaction", Guid.NewGuid(), new { });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task VerifyChainAsync_WhenThereAreNoEntries_IsValid()
    {
        var result = await CreateService().VerifyChainAsync();

        result.IsValid.Should().BeTrue();
        result.CheckedCount.Should().Be(0);
    }

    [Fact]
    public async Task VerifyChainAsync_WhenNothingHasBeenChanged_IsValid()
    {
        var service = CreateService();
        await AppendAndSaveAsync(service);
        await AppendAndSaveAsync(service);

        var result = await service.VerifyChainAsync();

        result.IsValid.Should().BeTrue();
        result.CheckedCount.Should().Be(2);
        result.FirstInvalidId.Should().BeNull();
    }

    [Fact]
    public async Task VerifyChainAsync_WhenAnEntryWasChanged_ReportsThatEntry()
    {
        var service = CreateService();
        await AppendAndSaveAsync(service);
        await AppendAndSaveAsync(service);
        await AppendAndSaveAsync(service);
        var changed = (await _context.AuditEntries.OrderBy(a => a.Id).ToListAsync())[1];
        changed.Details = "{\"amount\":\"999999.00\"}";
        await _context.SaveChangesAsync();

        var result = await service.VerifyChainAsync();

        result.IsValid.Should().BeFalse();
        result.FirstInvalidId.Should().Be(changed.Id);
        result.CheckedCount.Should().Be(1);
    }

    [Fact]
    public async Task VerifyChainAsync_WhenAnEntryWasRemoved_ReportsTheEntryAfterIt()
    {
        var service = CreateService();
        await AppendAndSaveAsync(service);
        await AppendAndSaveAsync(service);
        await AppendAndSaveAsync(service);
        var entries = await _context.AuditEntries.OrderBy(a => a.Id).ToListAsync();
        _context.AuditEntries.Remove(entries[1]);
        await _context.SaveChangesAsync();

        var result = await service.VerifyChainAsync();

        result.IsValid.Should().BeFalse();
        result.FirstInvalidId.Should().Be(entries[2].Id);
        result.CheckedCount.Should().Be(1);
    }

    [Fact]
    public async Task VerifyChainAsync_WithADifferentKey_IsInvalid()
    {
        await AppendAndSaveAsync(CreateService());

        var result = await new AuditService(_context, new AuditKey(new byte[32])).VerifyChainAsync();

        result.IsValid.Should().BeFalse();
    }
    [Fact]
    public async Task EnsureKeyAsync_WhenEntriesExistButTheKeyIsMissing_ThrowsInsteadOfCreatingANewKey()
    {
        await AppendAndSaveAsync(CreateService());
        var missingKey = AuditKey.Load(EmptyConfiguration(), KeyFilePath);

        var act = () => new AuditService(_context, missingKey).EnsureKeyAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Refusing to generate a new key*");
        missingKey.IsAvailable.Should().BeFalse();
        File.Exists(KeyFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task EnsureKeyAsync_WhenTheAuditLogIsEmpty_CreatesTheKeyFile()
    {
        var key = AuditKey.Load(EmptyConfiguration(), KeyFilePath);

        await new AuditService(_context, key).EnsureKeyAsync();

        key.IsAvailable.Should().BeTrue();
        File.Exists(KeyFilePath).Should().BeTrue();
        AuditKey.Load(EmptyConfiguration(), KeyFilePath).Bytes.Should().Equal(key.Bytes);
    }

    [Fact]
    public async Task EnsureKeyAsync_WhenTheKeyExists_LeavesItAlone()
    {
        await AppendAndSaveAsync(CreateService());

        var act = () => CreateService().EnsureKeyAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Load_ReadsTheKeyFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Audit:Key"] = Convert.ToBase64String(Key) })
            .Build();

        var key = AuditKey.Load(configuration, KeyFilePath);

        key.IsAvailable.Should().BeTrue();
        key.Bytes.Should().Equal(Key);
    }

    [Fact]
    public void Load_WhenThereIsNoKey_LeavesItUnavailable()
    {
        var key = AuditKey.Load(EmptyConfiguration(), KeyFilePath);

        key.IsAvailable.Should().BeFalse();
        var act = () => key.Bytes;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AuditKey_WhenShorterThan32Bytes_Throws()
    {
        var act = () => new AuditKey(new byte[31]);

        act.Should().Throw<InvalidOperationException>();
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private AuditService CreateService() => new(_context, new AuditKey(Key));

    private async Task AppendAndSaveAsync(
        AuditService service,
        Guid? userId = null,
        string action = "DEPOSIT",
        Guid? entityId = null,
        object? details = null)
    {
        await service.AppendAsync(userId, action, "Transaction", entityId ?? Guid.NewGuid(), details ?? new { });
        await _context.SaveChangesAsync();
    }
}
