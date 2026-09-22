namespace NordiskaPortal.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using NordiskaPortal.API.Services;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(TransactionService transactionService, ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        try
        {
            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than 0" });

            var transaction = await _transactionService.ProcessDepositAsync(request.AccountId, request.Amount);

            _logger.LogInformation($"Deposit processed for account {request.AccountId}");
            return CreatedAtAction(nameof(Deposit), new { id = transaction.Id }, new
            {
                transactionId = transaction.Id,
                accountId = transaction.AccountId,
                amount = transaction.Amount,
                status = transaction.Status,
                message = "Deposit successful. Notification queued for sending."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deposit failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("withdrawal")]
    public async Task<IActionResult> Withdrawal([FromBody] WithdrawalRequest request)
    {
        try
        {
            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than 0" });

            var transaction = await _transactionService.ProcessWithdrawalAsync(request.AccountId, request.Amount);

            _logger.LogInformation($"Withdrawal processed for account {request.AccountId}");
            return CreatedAtAction(nameof(Withdrawal), new { id = transaction.Id }, new
            {
                transactionId = transaction.Id,
                accountId = transaction.AccountId,
                amount = transaction.Amount,
                status = transaction.Status,
                message = "Withdrawal successful. Notification queued for sending."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Withdrawal failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    public class DepositRequest
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
    }

    public class WithdrawalRequest
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}