using System.Security.Claims;
using HotelManagement.Data;
using HotelManagement.DTOs;
using HotelManagement.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace HotelManagement.Controllers;


[ApiController ]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RoomsController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet("hotel/{hotelId}")]
    public async Task<IActionResult> GetByHotel(int hotelId)
    {
        var rooms = await _context.Rooms
            .Include(r => r.Hotel)
            .Where(r => r.HotelId == hotelId)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                HotelId = r.HotelId,
                HotelName = r.Hotel!.Name,
                RoomNumber = r.RoomNumber,
                Type = r.Type.ToString(),
                PricePerNight = r.PricePerNight,
                Capacity = r.Capacity,
                IsAvailable = r.IsAvailable
            })
            .ToListAsync();
        
        return Ok(rooms);
    }



    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(RoomCreateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var hotel = await _context.Hotels.FindAsync(dto.HotelId);


        if (hotel == null)
            return NotFound("Otel bulunamadı.");


        if (hotel.OwnerId != userId)
            return Forbid();

        var room = new Room
        {
            HotelId = dto.HotelId,
            RoomNumber = dto.RoomNumber,
            Type = dto.Type,
            PricePerNight = dto.PricePerNight,
            Capacity = dto.Capacity,
            IsAvailable = true

        };
        
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetByHotel), new { hotelId = room.HotelId },
            new { room.Id, room.RoomNumber });


    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, RoomUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
            return NotFound("Oda bulunamadı.");
        if (room.Hotel!.OwnerId != userId)
            return Forbid();
        
        room.RoomNumber = dto.RoomNumber;
        room.Type = dto.Type;
        room.PricePerNight = dto.PricePerNight;
        room.Capacity = dto.Capacity;
        
        await _context.SaveChangesAsync();
        return NoContent();

    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if(room == null)
            return NotFound("Oda bulunamadı.");

        if (room.Hotel!.OwnerId != userId)
            return Forbid();
        
        _context.Remove(room);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    

}