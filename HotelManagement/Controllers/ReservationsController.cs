using System.Security.Claims;
using HotelManagement.DTOs;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    private int GetUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

  
    [Authorize(Roles = "Customer,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(ReservationCreateDto dto)
        => Ok(await _reservationService.CreateAsync(GetUserId(), dto));

    
    [Authorize(Roles = "Customer,Admin")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
        => Ok(await _reservationService.GetMyAsync(GetUserId()));
    

    [Authorize(Roles = "Customer,Admin")]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
        => Ok(await _reservationService.CancelAsync(GetUserId(), id));
    
    
    [Authorize(Roles = "Customer,Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await _reservationService.GetByIdAsync(GetUserId(), User.IsInRole("Admin"), id));

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _reservationService.GetAllAsync(page, pageSize));

    [Authorize(Roles = "Customer,Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ReservationCreateDto dto)
        => Ok(await _reservationService.UpdateAsync(GetUserId(), id, dto));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _reservationService.DeleteAsync(id);
        return NoContent();
    }


}