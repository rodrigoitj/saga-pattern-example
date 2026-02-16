namespace Car.API.Application.DTOs;

public class CreateCarRentalRequestDto
{
    public Guid UserId { get; init; }
    public string CarModel { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public DateTime PickUpDate { get; init; }
    public DateTime ReturnDate { get; init; }
    public string PickUpLocation { get; init; } = string.Empty;
    public decimal PricePerDay { get; init; }
}

public class CarRentalResponseDto
{
    public Guid Id { get; init; }
    public string ReservationCode { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string CarModel { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public DateTime PickUpDate { get; init; }
    public DateTime ReturnDate { get; init; }
    public string PickUpLocation { get; init; } = string.Empty;
    public decimal PricePerDay { get; init; }
    public decimal TotalPrice { get; init; }
    public string Status { get; init; } = string.Empty;
}
