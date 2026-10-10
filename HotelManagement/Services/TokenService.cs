using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelManagement.Entities;
using Microsoft.IdentityModel.Tokens;



namespace HotelManagement.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(User user, List<string> roles)
    {
        //token içine koyulan bilgiler:
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        //kullanıcının rolü 
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        
        //secret keyi al imza 
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        
        // token oluştrma
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience:_configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiresInMinutes"]!)),
            signingCredentials: creds);

        
        return new JwtSecurityTokenHandler().WriteToken(token);

    }
}