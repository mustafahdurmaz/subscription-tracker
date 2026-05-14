// /api/customers endpoint'leri.
// İş mantığı yok — sadece HTTP -> Service çevirisi yapar.
// Service null dönerse 404, yoksa uygun durum kodu.
using Microsoft.AspNetCore.Mvc;
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Services;

namespace SubscriptionTracker.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponseDto>> Create([FromBody] CustomerCreateDto dto)
    {
        var result = await _customerService.CreateAsync(dto);

        return result.Outcome switch
        {
            CustomerCreateOutcome.Success
                => CreatedAtAction(nameof(GetById), new { id = result.Customer!.Id }, result.Customer),
            CustomerCreateOutcome.EmailAlreadyExists
                => Conflict(new { error = "Bu e-posta adresiyle kayıtlı bir müşteri zaten var." }),
            _ => StatusCode(500, new { error = "Beklenmeyen durum." })
        };
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponseDto>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponseDto>> GetById(Guid id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null) return NotFound(new { error = "Müşteri bulunamadı." });
        return Ok(customer);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _customerService.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Müşteri bulunamadı." });
        return NoContent();
    }

    // Müşteri özeti: aktif abonelikler + bu ay ödenmemişler + son 10 ödeme.
    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<CustomerSummaryDto>> Summary(Guid id)
    {
        var summary = await _customerService.GetSummaryAsync(id);
        if (summary is null) return NotFound(new { error = "Müşteri bulunamadı." });
        return Ok(summary);
    }
}
