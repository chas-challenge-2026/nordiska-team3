using NordiskaPortal.API.DTOs.Accounts;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IInterestService
{
    Task<AccountsResponseDto> AddInterestAsync(AccountsResponseDto accounts);
}