using System.Security.Claims;
using HotelManagement.DTOs;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    private int GetUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("hotel/{hotelId}")]
    public async Task<IActionResult> GetByHotel(int hotelId)
        => Ok(await _roomService.GetByHotelAsync(hotelId));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(RoomCreateDto dto)
    {
        var created = await _roomService.CreateAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetByHotel),
            new { hotelId = created.HotelId }, created);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, RoomUpdateDto dto)
    {
        await _roomService.UpdateAsync(GetUserId(), id, dto);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }
}