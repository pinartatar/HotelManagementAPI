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
}