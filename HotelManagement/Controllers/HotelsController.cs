using System.Security.Claims;
using HotelManagement.Data;
using HotelManagement.DTOs;
using HotelManagement.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace HotelManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<HotelsController> _logger;
    
    

    public HotelsController(AppDbContext context, ILogger<HotelsController> logger)
    {
        _context = context;
        _logger = logger;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var hotels = await _context.Hotels
            .Include(h => h.Owner)
            .Select(h => new HotelDto
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City,
                Address = h.Address,
                OwnerName = h.Owner!.FullName
            })
            .ToListAsync();
        
        return Ok(hotels);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var hotel = await _context.Hotels
            .Include(h => h.Owner)
            .FirstOrDefaultAsync(h => h.Id == id);


        if (hotel == null)
            return NotFound("Otel bulunamadı.");
        
        return Ok(new HotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            City = hotel.City,
            Address = hotel.Address,
            OwnerName = hotel.Owner!.FullName
        });

    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(HotelCreateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var hotel = new Hotel
        {
            Name = dto.Name,
            City = dto.City,
            Address = dto.Address,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Yeni otel oluşturuldu: {HotelName} (Id: {HotelId}, sahip: {UserId})",
            hotel.Name, hotel.Id, userId);
        
        return CreatedAtAction(nameof(GetById), new { id = hotel.Id },
            new { hotel.Id, hotel.Name, hotel.City });
    }


    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, HotelCreateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel == null)
            return NotFound("Otel bulunamadı.");
        
        if(hotel.OwnerId != userId)
            return Forbid();
        
        hotel.Name = dto.Name;
        hotel.City = dto.City;
        hotel.Address = dto.Address;
        
        await _context.SaveChangesAsync();
        return NoContent();

    }


    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel == null)
            return NotFound("Otel bulunamadı.");

        if (hotel.OwnerId != userId)
            return Forbid();
        
        _context.Hotels.Remove(hotel);
        await _context.SaveChangesAsync();
        return NoContent();

    }


}