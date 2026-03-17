namespace CarRentalService.Models.DTOs;

public class QueryResponse<T>
{
    public int TotalElements { get; set; }
    public IEnumerable<T> Elements { get; set; } = new List<T>();
}