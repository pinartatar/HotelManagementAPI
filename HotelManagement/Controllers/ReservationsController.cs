using System.Security.Claims;
using HotelManagement.Data;
using HotelManagement.DTOs;
using HotelManagement.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace HotelManagement.Controllers;
[ApiController]
[Route("api/[controller]")]

public class ReservationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReservationsController> _logger;
    
    public  ReservationsController(AppDbContext context, ILogger<ReservationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(ReservationCreateDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        //tarihlere bakıyoruz 
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.CheckInDate < today)
            return BadRequest("Geçmiş tarihe rezervasyon yapılamaz.");
        
        if (dto.CheckOutDate <= dto.CheckInDate)
            return BadRequest("Çıkış tarihi Giriş tarihinden sonra olmalıdır.");


        //oda var mı 
        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == dto.RoomId);

        if (room == null)
            return NotFound("Oda bulunamadı!");

        
        var isOccupied = await _context.Reservations.AnyAsync(r =>
            r.RoomId == dto.RoomId &&
            r.Status != ReservationStatus.Cancelled && //iptal olan rezi alma 
            dto.CheckInDate < r.CheckOutDate &&
            dto.CheckOutDate > r.CheckInDate);
        if (isOccupied)
            return Conflict("Oda bu tarihlerde dolu.");
        
        //fiyat
        var nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
        var totalPrice = nights * room.PricePerNight;

        //bakiye hesaplama
        var user = await _context.Users.FindAsync(userId);
        if(user!.Balance < totalPrice)
            return BadRequest($"Yetersiz bakiye. Gerekli: {totalPrice}, mevcut: {user.Balance}");
        user.Balance -= totalPrice; //bakiyeden tutarı düş çıkar azalt
        
        
        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = userId,
            CheckInDate = dto.CheckInDate,
            CheckOutDate = dto.CheckOutDate,
            TotalPrice = totalPrice,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Reservations.Add(reservation); 
       

        
        var payment = new Payment
        {
            Reservation = reservation,
            Amount = totalPrice,
            PaymentDate = DateTime.UtcNow,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Paid
        };
        
        _context.Payments.Add(payment);
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("Rezervasyon oluşturuldu: Oda {RoomId}, kullanıcı {userId}, {Nights} gece , tutar {Total}", room.Id, user.Id, nights, totalPrice );

        return Ok(new { reservation.Id, totalPrice, nights });
        
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

   
        
        var reservation = await _context.Reservations
            .Include(r => r.Room)
            .ThenInclude(room =>room!.Hotel )
            .Where(r => r.UserId == userId)
            .Select(r => new ReservationDto
        {
            Id = r.Id,
            HotelName = r.Room!.Hotel!.Name,
            RoomNumber = r.Room.RoomNumber,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            TotalPrice = r.TotalPrice,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt
        })
        .ToListAsync();
        return Ok(reservation);


    }

    [Authorize]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return NotFound("Reservasyon bulunamadı.");

        if (reservation.UserId != userId)
            return Forbid();
        if (reservation.Status == ReservationStatus.Cancelled)
            return BadRequest("Bu rezervasyon zaten iptal edilmiş.");
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (today >= reservation.CheckInDate)
            return BadRequest("başlamış veya geçmiş rezervasyon iptal edilemez.");
        
        //iptal edilecel tutar
        var daysUntilCheckIn = reservation.CheckInDate.DayNumber - today.DayNumber;
        var penaltyRate = daysUntilCheckIn < 2 ? 0.20m : 0m;
        var penalty = reservation.TotalPrice * penaltyRate;
        var refund = reservation.TotalPrice - penalty;  
        

        var user = await _context.Users.FindAsync(userId); 
        user!.Balance += refund; //iade yükle
        
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CanceledAt = DateTime.UtcNow;
        reservation.PenaltyAmount = penalty;

        var refundPayment = new Payment
        {
            ReservationId = reservation.Id,
            Amount = refund,
            PaymentDate = DateTime.UtcNow,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Refunded
        };
        _context.Payments.Add(refundPayment);
        
        await _context.SaveChangesAsync();
        

        _logger.LogInformation("Rezervasyon iptal edildi: {ReservationId}, ceza {Penalty}, iade {Refund}",
            reservation.Id, penalty, refund);
        return Ok(new { refund, penalty, daysUntilCheckIn });


    }
    






}