using System.Collections.Generic;
using System.Threading.Tasks;
using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Services.Interfaces
{
    public interface IFaqService
    {
        Task<FaqSearchResultDto> SearchFaqAsync(string query);
        Task<IEnumerable<FaqEntry>> GetDefaultFaqsAsync(int count = 6);
    }
}