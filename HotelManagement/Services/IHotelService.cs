using HotelManagement.DTOs;

namespace HotelManagement.Services;

public interface IHotelService
{
    Task<List<HotelDto>> GetAllAsync();
    Task<HotelDto> GetByIdAsync(int id);
    Task<HotelDto> CreateAsync(int userId, HotelCreateDto dto);
    Task UpdateAsync(int userId, int id, HotelCreateDto dto);
    Task DeleteAsync(int userId, int id);
}