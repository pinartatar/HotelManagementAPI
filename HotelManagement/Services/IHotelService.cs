using HotelManagement.DTOs;

namespace HotelManagement.Services;

public interface IHotelService
{
    Task<PagedResultDto<HotelDto>> GetAllAsync(string? city, string? search, int page, int pageSize);
    Task<HotelDto> GetByIdAsync(int id);
    Task<HotelDto> CreateAsync(int userId, HotelCreateDto dto);
    Task UpdateAsync(int userId, bool isAdmin, int id, HotelCreateDto dto);
    Task DeleteAsync(int userId, bool isAdmin, int id);
}