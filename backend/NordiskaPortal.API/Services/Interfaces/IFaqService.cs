using NordiskaPortal.API.DTOs.Faq;

namespace NordiskaPortal.API.Services.Interfaces;

public interface IFaqService
{
    Task<FaqSearchResultDto> SearchFaqAsync(string query);
}