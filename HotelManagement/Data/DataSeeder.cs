using HotelManagement.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        
        // 1) admin müşteri owner 
        
        string[] roleNames = { "Admin", "Owner", "Customer" };
        foreach (var name in roleNames)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == name))
                context.Roles.Add(new Role { Name = name });
        }
        await context.SaveChangesAsync();
        

        // 2)admin belirledik
        if (!await context.Users.AnyAsync(u => u.Email == "admin@hotel.com"))
        {
            
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var admin = new User
            {
                
                FullName = "System Admin",
                Email = "admin@hotel.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };
            
            admin.UserRoles.Add(new UserRole { User = admin, Role = adminRole });
            
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
        
        
    }
}