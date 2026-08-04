using HotelManagement.DTOs;

namespace HotelManagement.Services;

public interface IRoomService
{
    Task<List<RoomDto>> GetByHotelAsync(int hotelId);
    Task<RoomDto> CreateAsync(int userId, RoomCreateDto dto);
    Task UpdateAsync(int userId, int id, RoomUpdateDto dto);
    Task DeleteAsync(int userId, int id);
}