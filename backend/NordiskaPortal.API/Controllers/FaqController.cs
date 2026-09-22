using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaqController : ControllerBase
{
    private readonly IFaqService _faqService;

    public FaqController(IFaqService faqService)
    {
        _faqService = faqService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<FaqSearchResultDto>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Söksträngen kan inte vara tom." });
        }

        var result = await _faqService.SearchFaqAsync(query);
        return Ok(result);
    }
}