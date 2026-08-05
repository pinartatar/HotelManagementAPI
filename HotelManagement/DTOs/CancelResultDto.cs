namespace HotelManagement.DTOs;

public class CancelResultDto
{
    public decimal Refund { get; set; }
    public decimal Penalty { get; set; }
    public int DaysUntilCheckIn { get; set; }
}
