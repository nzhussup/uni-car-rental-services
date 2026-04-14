using System.ComponentModel.DataAnnotations;

namespace BookingService.Models.DTOs;

public class PaginationDto
{
    [Required]
    [Range(0, int.MaxValue)]
    public int Skip { get; set; }

    [Required]
    [Range(0, 50)]
    public int Take { get; set; }
}