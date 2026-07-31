namespace HotelManagement.Entities;

public class Reservation
{
    public int Id { get; set; }
    
    //oda ilişkisi
    public int RoomId { get; set; }
    public Room? Room { get; set; } 
    
    //kullanıcı ilişkisi
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; }
    
    //iptal takibi ceza 
    public DateTime? CanceledAt { get; set; }
    public decimal PenaltyAmount { get; set; }
    
    public DateTime CreatedAt { get; set; }
  
    
    
    
}