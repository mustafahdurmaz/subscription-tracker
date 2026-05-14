// /api/subscriptions endpoint'leri.
// GET'te ?customerId= ile filtreleme. POST'ta customer yoksa service null döner → 404.
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Services;

namespace SubscriptionTracker.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionResponseDto>> Create([FromBody] SubscriptionCreateDto dto)
    {
        var result = await _subscriptionService.CreateAsync(dto);

        return result.Outcome switch
        {
            SubscriptionCreateOutcome.Success
                => CreatedAtAction(nameof(GetById), new { id = result.Subscription!.Id }, result.Subscription),
            SubscriptionCreateOutcome.CustomerNotFound
                => NotFound(new { error = "Müşteri bulunamadı." }),
            SubscriptionCreateOutcome.DuplicateSubscriptionNumber
                => Conflict(new { error = "Bu abonelik numarası zaten kayıtlı." }),
            _ => StatusCode(500, new { error = "Beklenmeyen durum." })
        };
    }

    [HttpGet]
    public async Task<ActionResult<List<SubscriptionResponseDto>>> GetAll([FromQuery] Guid? customerId)
    {
        var subscriptions = await _subscriptionService.GetAllAsync(customerId);
        return Ok(subscriptions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionResponseDto>> GetById(Guid id)
    {
        var subscription = await _subscriptionService.GetByIdAsync(id);
        if (subscription is null) return NotFound(new { error = "Abonelik bulunamadı." });
        return Ok(subscription);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionResponseDto>> Update(Guid id, [FromBody] SubscriptionUpdateDto dto)
    {
        var updated = await _subscriptionService.UpdateAsync(id, dto);
        if (updated is null) return NotFound(new { error = "Abonelik bulunamadı." });
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _subscriptionService.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Abonelik bulunamadı." });
        return NoContent();
    }

    // Mock borç sorgulama — DebtInquiryService'i çağırır, abonelik için
    // tutar + son ödeme tarihi + dönem döner.
    [HttpGet("{id:guid}/debt-inquiry")]
    public async Task<IActionResult> DebtInquiry(Guid id)
    {
        var debt = await _subscriptionService.GetDebtInquiryAsync(id);
        if (debt is null) return NotFound(new { error = "Abonelik bulunamadı." });
        return Ok(debt);
    }
}
