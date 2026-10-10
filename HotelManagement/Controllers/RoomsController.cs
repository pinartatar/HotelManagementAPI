using System.Security.Claims;
using HotelManagement.DTOs;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Entities;

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
    public async Task<IActionResult> GetByHotel(
        int hotelId,
        [FromQuery] RoomType? type,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _roomService.GetByHotelAsync(hotelId, type, maxPrice, page, pageSize));
    
    
    [Authorize(Roles = "Owner,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(RoomCreateDto dto)
    {
        
        var created = await _roomService.CreateAsync(GetUserId(), dto);
        
        return CreatedAtAction(nameof(GetByHotel),
            new { hotelId = created.HotelId }, created);
    }
    

    [Authorize(Roles = "Owner,Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, RoomUpdateDto dto)
    {
        
        await _roomService.UpdateAsync(GetUserId(), User.IsInRole("Admin"), id, dto);
        return NoContent();
    }

    
    [Authorize(Roles = "Owner,Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(GetUserId(), User.IsInRole("Admin"), id);
        return NoContent();
    }
}