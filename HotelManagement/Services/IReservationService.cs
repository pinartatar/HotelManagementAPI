using HotelManagement.DTOs;

namespace HotelManagement.Services;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(int userId, ReservationCreateDto dto);
    Task<List<ReservationDto>> GetMyAsync(int userId);
    Task<CancelResultDto> CancelAsync(int userId, int id);
}