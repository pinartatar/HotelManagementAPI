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
            .ThenInclude(h => h!.Owner) 
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
        
        room.Hotel!.Owner!.Balance += totalPrice;   // para otel sahibine geçer

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
            .Include(r => r.Room)
            .ThenInclude(room => room!.Hotel)
            .ThenInclude(h => h!.Owner)
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
        reservation.Room!.Hotel!.Owner!.Balance -= refund;   // sahibi iadeyi öder, ceza (200) onda kalır

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
    
    public async Task<ReservationDto> GetByIdAsync(int userId, bool isAdmin, int id)
{
    var reservation = await _context.Reservations
        .Include(r => r.Room).ThenInclude(room => room!.Hotel)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reservation == null)
        throw new NotFoundException("Rezervasyon bulunamadı.");

    if (!isAdmin && reservation.UserId != userId)
        throw new ForbiddenException("Bu rezervasyon size ait değil.");

    return new ReservationDto
    {
        Id = reservation.Id,
        HotelName = reservation.Room!.Hotel!.Name,
        RoomNumber = reservation.Room.RoomNumber,
        CheckInDate = reservation.CheckInDate,
        CheckOutDate = reservation.CheckOutDate,
        TotalPrice = reservation.TotalPrice,
        Status = reservation.Status.ToString(),
        CreatedAt = reservation.CreatedAt
    };
}

public async Task<PagedResultDto<ReservationDto>> GetAllAsync(int page, int pageSize)
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 50) pageSize = 10;

    var query = _context.Reservations
        .Include(r => r.Room).ThenInclude(room => room!.Hotel)
        .AsQueryable();

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderByDescending(r => r.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
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

    return new PagedResultDto<ReservationDto>
    {
        Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
    };
}

public async Task<ReservationDto> UpdateAsync(int userId, int id, ReservationCreateDto dto)
{
    var reservation = await _context.Reservations
        .Include(r => r.Room).ThenInclude(room => room!.Hotel)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reservation == null)
        throw new NotFoundException("Rezervasyon bulunamadı.");

    if (reservation.UserId != userId)
        throw new ForbiddenException("Bu rezervasyon size ait değil.");

    if (reservation.Status == ReservationStatus.Cancelled)
        throw new BadRequestException("İptal edilmiş rezervasyon güncellenemez.");

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    if (today >= reservation.CheckInDate)
        throw new BadRequestException("Başlamış konaklama güncellenemez.");
    if (dto.CheckInDate < today)
        throw new BadRequestException("Geçmiş tarihe rezervasyon yapılamaz.");
    if (dto.CheckOutDate <= dto.CheckInDate)
        throw new BadRequestException("Çıkış tarihi giriş tarihinden sonra olmalı.");

    // Çakışma kontrolü — KENDİSİ HARİÇ! (r.Id != id şartına dikkat)
    var isOccupied = await _context.Reservations.AnyAsync(r =>
        r.Id != id &&
        r.RoomId == reservation.RoomId &&
        r.Status != ReservationStatus.Cancelled &&
        dto.CheckInDate < r.CheckOutDate &&
        dto.CheckOutDate > r.CheckInDate);
    if (isOccupied)
        throw new ConflictException("Oda yeni tarihlerde dolu.");

    // Yeni fiyat ve fark hesabı
    var nights = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;
    var newPrice = nights * reservation.Room!.PricePerNight;
    var difference = newPrice - reservation.TotalPrice; // + = ek ücret, - = iade

    var user = await _context.Users.FindAsync(userId);
    if (difference > 0 && user!.Balance < difference)
        throw new BadRequestException(
            $"Yetersiz bakiye. Ek ücret: {difference}, mevcut: {user.Balance}");

    user!.Balance -= difference; // fark artıysa düşer, eksiyse iade olur

    if (difference != 0)
    {
        _context.Payments.Add(new Payment
        {
            ReservationId = reservation.Id,
            Amount = Math.Abs(difference),
            PaymentDate = DateTime.UtcNow,
            Method = PaymentMethod.Card,
            Status = difference > 0 ? PaymentStatus.Paid : PaymentStatus.Refunded
        });
    }

    reservation.CheckInDate = dto.CheckInDate;
    reservation.CheckOutDate = dto.CheckOutDate;
    reservation.TotalPrice = newPrice;

    await _context.SaveChangesAsync();

    _logger.LogInformation(
        "Rezervasyon güncellendi: {ReservationId}, yeni tutar {NewPrice}, fark {Difference}",
        reservation.Id, newPrice, difference);

    return new ReservationDto
    {
        Id = reservation.Id,
        HotelName = reservation.Room.Hotel!.Name,
        RoomNumber = reservation.Room.RoomNumber,
        CheckInDate = reservation.CheckInDate,
        CheckOutDate = reservation.CheckOutDate,
        TotalPrice = reservation.TotalPrice,
        Status = reservation.Status.ToString(),
        CreatedAt = reservation.CreatedAt
    };
}

public async Task DeleteAsync(int id)
{
    var reservation = await _context.Reservations
        .Include(r => r.Room)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (reservation == null)
        throw new NotFoundException("Rezervasyon bulunamadı.");

    var payments = await _context.Payments
        .Where(p => p.ReservationId == id).ToListAsync();
    _context.Payments.RemoveRange(payments);

    _context.Reservations.Remove(reservation);
    await _context.SaveChangesAsync();

    _logger.LogInformation("Rezervasyon silindi (admin): {ReservationId}", id);
}
}