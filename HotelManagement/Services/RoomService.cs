using HotelManagement.Data;
using HotelManagement.DTOs;
using HotelManagement.Entities;
using HotelManagement.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RoomService> _logger;

    public RoomService(AppDbContext context, ILogger<RoomService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoomDto>> GetByHotelAsync(int hotelId)
    {
        return await _context.Rooms
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
    }

    public async Task<RoomDto> CreateAsync(int userId, RoomCreateDto dto)
    {
        var hotel = await _context.Hotels.FindAsync(dto.HotelId);
        if (hotel == null)
            throw new NotFoundException("Otel bulunamadı.");

        if (hotel.OwnerId != userId)
            throw new ForbiddenException("Bu otel size ait değil.");

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

        _logger.LogInformation("Yeni oda eklendi: {RoomNumber} (otel: {HotelId})",
            room.RoomNumber, room.HotelId);

        return new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            HotelName = hotel.Name,
            RoomNumber = room.RoomNumber,
            Type = room.Type.ToString(),
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            IsAvailable = room.IsAvailable
        };
    }

    public async Task UpdateAsync(int userId, bool isAdmin, int id, RoomUpdateDto dto)
    {
        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
            throw new NotFoundException("Oda bulunamadı.");

        if (!isAdmin && room.Hotel!.OwnerId != userId)
            throw new ForbiddenException("Bu odanın oteli size ait değil.");

        room.RoomNumber = dto.RoomNumber;
        room.Type = dto.Type;
        room.PricePerNight = dto.PricePerNight;
        room.Capacity = dto.Capacity;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId, bool isAdmin, int id)
    {
        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
            throw new NotFoundException("Oda bulunamadı.");

        if (!isAdmin && room.Hotel!.OwnerId != userId)
            throw new ForbiddenException("Bu odanın oteli size ait değil.");

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Oda silindi: {RoomId}", id);
    }
}