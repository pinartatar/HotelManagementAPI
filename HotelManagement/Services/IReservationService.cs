using HotelManagement.DTOs;

namespace HotelManagement.Services;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(int userId, ReservationCreateDto dto);
    Task<List<ReservationDto>> GetMyAsync(int userId);
    Task<CancelResultDto> CancelAsync(int userId, int id);
    Task<ReservationDto> GetByIdAsync(int userId, bool isAdmin, int id);
    Task<PagedResultDto<ReservationDto>> GetAllAsync(int page, int pageSize);
    Task<ReservationDto> UpdateAsync(int userId, int id, ReservationCreateDto dto);
    Task DeleteAsync(int id);
}