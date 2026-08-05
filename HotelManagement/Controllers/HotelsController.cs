using System.Security.Claims;
using HotelManagement.DTOs;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    private int GetUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _hotelService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await _hotelService.GetByIdAsync(id));

    [Authorize(Roles = "Owner,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(HotelCreateDto dto)
    {
        var created = await _hotelService.CreateAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, HotelCreateDto dto)
    {
        await _hotelService.UpdateAsync(GetUserId(), User.IsInRole("Admin"), id, dto);
        return NoContent();
    }

    [Authorize(Roles = "Owner,Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _hotelService.DeleteAsync(GetUserId(), User.IsInRole("Admin"), id);
        return NoContent();
    }
}