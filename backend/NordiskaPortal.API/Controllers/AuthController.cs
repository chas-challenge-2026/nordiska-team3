using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.Data;

namespace NordiskaPortal.API.Controllers;
// Skriv om controller då vi ska byta från password till PIN!
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
};