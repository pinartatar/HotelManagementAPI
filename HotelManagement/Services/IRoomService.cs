using HotelManagement.DTOs;
using HotelManagement.Entities;

namespace HotelManagement.Services;

public interface IRoomService
{
    Task<PagedResultDto<RoomDto>> GetByHotelAsync(int hotelId, RoomType? type, decimal? maxPrice, int page, int pageSize);
    Task<RoomDto> CreateAsync(int userId, RoomCreateDto dto);
    Task UpdateAsync(int userId, bool isAdmin, int id, RoomUpdateDto dto);
    Task DeleteAsync(int userId, bool isAdmin, int id);
}