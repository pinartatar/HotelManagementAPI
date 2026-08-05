using HotelManagement.Data;
using HotelManagement.DTOs;
using HotelManagement.Entities;
using HotelManagement.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthService(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new ConflictException("Bu email zaten kayıtlı.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        var roleName = dto.Role.Trim();
        if (roleName != "Customer" && roleName != "Owner")
            throw new BadRequestException("Rol yalnızca 'Customer' veya 'Owner' olabilir.");

        var role = await _context.Roles.FirstAsync(r => r.Name == roleName);
        user.UserRoles.Add(new UserRole { User = user, Role = role });

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.CreateToken(user, new List<string> { roleName });
        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedException("Email veya şifre hatalı.");

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var token = _tokenService.CreateToken(user, roles);
        return new AuthResponseDto
        {
            Token = token, FullName = user.FullName, Email = user.Email
        };
    }
}