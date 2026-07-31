namespace HotelManagement.DTOs;

public class ReservationCreateDto
{
   
    public int RoomId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
}