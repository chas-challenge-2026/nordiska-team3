using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaqController : ControllerBase
    {
        private readonly IFaqService _faqService;

        public FaqController(IFaqService faqService)
        {
            _faqService = faqService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDefaultFaqs([FromQuery] int count = 6)
        {
            var faqs = await _faqService.GetDefaultFaqsAsync(count);

            var result = faqs.Select(f => new
            {
                f.Id,
                f.Question,
                f.Answer,
                f.Category
            });

            return Ok(result);
        }

        [HttpGet("search")]
        [EnableRateLimiting("SensitiveEndpointsPolicy")]
        public async Task<IActionResult> SearchFaq([FromQuery] string query)
        {
            var searchResult = await _faqService.SearchFaqAsync(query);
            return Ok(searchResult);
        }
    }
}