namespace HotelManagement.Entities;

public class Payment
{
    public int Id { get; set; }
    
    //rezervasyona ait ödeme
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    
    
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    
}