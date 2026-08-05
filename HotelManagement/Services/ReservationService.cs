using HotelManagement.Data;
using HotelManagement.DTOs;
using HotelManagement.Entities;
using HotelManagement.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(AppDbContext context, ILogger<ReservationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReservationDto> CreateAsync(int userId, ReservationCreateDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.CheckInDate < today)
            throw new BadRequestException("Geçmiş tarihe rezervasyon yapılamaz.");
        if (dto.CheckOutDate <= dto.CheckInDate)
            throw new BadRequestException("Çıkış tarihi giriş tarihinden sonra olmalı.");

        var room = await _context.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == dto.RoomId);
        if (room == null)
            throw new NotFoundException("Oda bulunamadı.");

        var isOccupied = await _context.Reservations.AnyAsync(r =>
            r.RoomId == dto.RoomId &&
            r.Status != ReservationStatus.Cancelled &&
            dto.CheckInDate < r.CheckOutDate &&
            dto.CheckOutDate > r.CheckInDate);
        if (isOccupied)
            throw new ConflictException("Oda bu tarihlerde dolu.");

        var nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
        var totalPrice = nights * room.PricePerNight;

        var user = await _context.Users.FindAsync(userId);
        if (user!.Balance < totalPrice)
            throw new BadRequestException(
                $"Yetersiz bakiye. Gerekli: {totalPrice}, mevcut: {user.Balance}");

        user.Balance -= totalPrice;

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

        _logger.LogInformation(
            "Rezervasyon oluşturuldu: Oda {RoomId}, kullanıcı {UserId}, {Nights} gece, tutar {Total}",
            room.Id, userId, nights, totalPrice);

        return new ReservationDto
        {
            Id = reservation.Id,
            HotelName = room.Hotel!.Name,
            RoomNumber = room.RoomNumber,
            CheckInDate = reservation.CheckInDate,
            CheckOutDate = reservation.CheckOutDate,
            TotalPrice = reservation.TotalPrice,
            Status = reservation.Status.ToString(),
            CreatedAt = reservation.CreatedAt
        };
    }

    public async Task<List<ReservationDto>> GetMyAsync(int userId)
    {
        return await _context.Reservations
            .Include(r => r.Room)
            .ThenInclude(room => room!.Hotel)
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
    }

    public async Task<CancelResultDto> CancelAsync(int userId, int id)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);
        if (reservation == null)
            throw new NotFoundException("Rezervasyon bulunamadı.");

        if (reservation.UserId != userId)
            throw new ForbiddenException("Bu rezervasyon size ait değil.");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new BadRequestException("Bu rezervasyon zaten iptal edilmiş.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today >= reservation.CheckInDate)
            throw new BadRequestException("Başlamış veya geçmiş konaklama iptal edilemez.");

        var daysUntilCheckIn = reservation.CheckInDate.DayNumber - today.DayNumber;
        var penaltyRate = daysUntilCheckIn < 2 ? 0.20m : 0m;
        var penalty = reservation.TotalPrice * penaltyRate;
        var refund = reservation.TotalPrice - penalty;

        var user = await _context.Users.FindAsync(userId);
        user!.Balance += refund;

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

        _logger.LogInformation(
            "Rezervasyon iptal edildi: {ReservationId}, ceza {Penalty}, iade {Refund}",
            reservation.Id, penalty, refund);

        return new CancelResultDto
        {
            Refund = refund,
            Penalty = penalty,
            DaysUntilCheckIn = daysUntilCheckIn
        };
    }
}