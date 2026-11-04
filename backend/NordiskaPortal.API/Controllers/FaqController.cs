using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.DTOs.Faq;
using NordiskaPortal.API.Services.Interfaces;

namespace NordiskaPortal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FaqController : ControllerBase
{
    private readonly IFaqService _faqService;

    public FaqController(IFaqService faqService)
    {
        _faqService = faqService;
    }

    // GET /api/faq
    [HttpGet]
    public async Task<IActionResult> GetFaqs()
    {
        return Ok(await _faqService.GetAllAsync());
    }

    // GET /api/faq/search?q=...
    [HttpGet("search")]
    public async Task<IActionResult> SearchFaq([FromQuery] FaqSearchRequestDto request)
    {
        return Ok(await _faqService.SearchAsync(request.Q!));
    }
}