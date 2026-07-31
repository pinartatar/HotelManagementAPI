using HotelManagement.Entities;

namespace HotelManagement.DTOs;

public class RoomUpdateDto
{
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
}