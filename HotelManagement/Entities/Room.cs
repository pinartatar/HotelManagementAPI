namespace HotelManagement.Entities;

public class Room
{
    public int Id { get; set; }
    
    public int HotelId  { get; set; } //her oda bir otele ait
    public Hotel? Hotel { get; set; }
    
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public decimal PricePerNight { get; set; }
    
    public int Capacity { get; set; }
    public bool IsAvailable { get; set; }
   
    
}