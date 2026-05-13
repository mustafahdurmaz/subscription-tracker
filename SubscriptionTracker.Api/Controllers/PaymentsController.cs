// /api/payments endpoint'leri.
// POST üç sonuç verebilir: 201 Created, 404 (abonelik yok), 409 (dönem ödenmiş).
// Service result.Outcome'a göre swicht ile uygun status koduna çeviriyoruz.
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Services;

namespace SubscriptionTracker.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> Create([FromBody] PaymentCreateDto dto)
    {
        var result = await _paymentService.CreateAsync(dto);

        return result.Outcome switch
        {
            PaymentCreateOutcome.Success
                => CreatedAtAction(nameof(GetById), new { id = result.Payment!.Id }, result.Payment),

            PaymentCreateOutcome.SubscriptionNotFound
                => NotFound(new { error = "Abonelik bulunamadı." }),

            PaymentCreateOutcome.PeriodAlreadyPaid
                => Conflict(new { error = "Bu dönem için zaten başarılı bir ödeme var." }),

            _ => StatusCode(500, new { error = "Beklenmeyen durum." })
        };
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentResponseDto>>> GetAll(
        [FromQuery] Guid? subscriptionId,
        [FromQuery] Guid? customerId)
    {
        var payments = await _paymentService.GetAllAsync(subscriptionId, customerId);
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponseDto>> GetById(Guid id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment is null) return NotFound(new { error = "Ödeme bulunamadı." });
        return Ok(payment);
    }
}
