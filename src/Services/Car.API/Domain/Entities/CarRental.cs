namespace Car.API.Domain.Entities;

using Shared.Domain.Abstractions;

public class CarRental : AggregateRoot
{
    public string ReservationCode { get; private set; } = string.Empty;
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public string CarModel { get; private set; } = string.Empty;
    public string Company { get; private set; } = string.Empty;
    public DateTime PickUpDate { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string PickUpLocation { get; private set; } = string.Empty;
    public decimal PricePerDay { get; private set; }
    public decimal TotalPrice { get; private set; }
    public CarRentalStatus Status { get; private set; } = CarRentalStatus.Pending;

    public static CarRental Create(
        Guid bookingId,
        Guid userId,
        string carModel,
        string company,
        DateTime pickUpDate,
        DateTime returnDate,
        string pickUpLocation,
        decimal pricePerDay
    )
    {
        var days = (returnDate - pickUpDate).Days;
        var totalPrice = pricePerDay * days;

        var carRental = new CarRental
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            UserId = userId,
            CarModel = carModel,
            Company = company,
            PickUpDate = pickUpDate,
            ReturnDate = returnDate,
            PickUpLocation = pickUpLocation,
            PricePerDay = pricePerDay,
            TotalPrice = totalPrice,
            ReservationCode =
                $"CR{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString()[..6].ToUpper()}",
            Status = CarRentalStatus.Pending,
        };
        return carRental;
    }

    public void Confirm()
    {
        if (Status != CarRentalStatus.Pending)
            throw new InvalidOperationException(
                "Car rental can only be confirmed from pending state"
            );

        Status = CarRentalStatus.Confirmed;
    }

    public void Cancel()
    {
        Status = CarRentalStatus.Cancelled;
    }
}

public enum CarRentalStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Failed,
}
