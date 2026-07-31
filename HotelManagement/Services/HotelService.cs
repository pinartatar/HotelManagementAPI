using HotelManagement.Data;
using HotelManagement.DTOs;
using HotelManagement.Entities;
using HotelManagement.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class HotelService : IHotelService
{
    private readonly AppDbContext _context;
    private readonly ILogger<HotelService> _logger;

    public HotelService(AppDbContext context, ILogger<HotelService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HotelDto> GetByIdAsync(int id)
    {
        var hotel = await _context.Hotels
            .Include(h => h.Owner)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hotel == null)
            throw new NotFoundException("Otel bulunamadı.");

        return new HotelDto
        {
            Id = hotel.Id, Name = hotel.Name, City = hotel.City,
            Address = hotel.Address, OwnerName = hotel.Owner!.FullName
        };
    }

    public async Task<HotelDto> CreateAsync(int userId, HotelCreateDto dto)
    {
        var hotel = new Hotel
        {
            Name = dto.Name, City = dto.City, Address = dto.Address,
            OwnerId = userId, CreatedAt = DateTime.UtcNow
        };

        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Yeni otel oluşturuldu: {HotelName} (Id: {HotelId}, sahip: {UserId})",
            hotel.Name, hotel.Id, userId);

        return new HotelDto
        {
            Id = hotel.Id, Name = hotel.Name, City = hotel.City,
            Address = hotel.Address, OwnerName = string.Empty
        };
    }

    public async Task<List<HotelDto>> GetAllAsync()
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
        
        return hotels;
    }
    
    public async Task UpdateAsync(int userId, int id, HotelCreateDto dto)
    {
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel == null)
            throw new NotFoundException("Otel bulunamadı.");

        if (hotel.OwnerId != userId)
            throw new ForbiddenException("Bu otel size ait değil.");

        hotel.Name = dto.Name;
        hotel.City = dto.City;
        hotel.Address = dto.Address;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var hotel = await _context.Hotels.FindAsync(id);
        if (hotel == null)
            throw new NotFoundException("Otel bulunamadı.");

        if (hotel.OwnerId != userId)
            throw new ForbiddenException("Bu otel size ait değil.");

        _context.Hotels.Remove(hotel);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Otel silindi: {HotelId} (sahip: {UserId})", id, userId);
    }
}