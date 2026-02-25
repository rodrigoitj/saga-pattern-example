namespace Hotel.API.Domain.Entities;

using Shared.Domain.Abstractions;

public class HotelBooking : AggregateRoot
{
    public string ConfirmationCode { get; private set; } = string.Empty;
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public string HotelName { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public DateTime CheckInDate { get; private set; }
    public DateTime CheckOutDate { get; private set; }
    public int RoomCount { get; private set; }
    public decimal PricePerNight { get; private set; }
    public decimal TotalPrice { get; private set; }
    public HotelBookingStatus Status { get; private set; } = HotelBookingStatus.Pending;

    public static HotelBooking Create(
        Guid bookingId,
        Guid userId,
        string hotelName,
        string city,
        DateTime checkInDate,
        DateTime checkOutDate,
        int roomCount,
        decimal pricePerNight
    )
    {
        var nights = (checkOutDate - checkInDate).Days;
        var totalPrice = pricePerNight * nights * roomCount;

        var hotelBooking = new HotelBooking
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            UserId = userId,
            HotelName = hotelName,
            City = city,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            RoomCount = roomCount,
            PricePerNight = pricePerNight,
            TotalPrice = totalPrice,
            ConfirmationCode =
                $"HT{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString()[..6].ToUpper()}",
            Status = HotelBookingStatus.Pending,
        };
        return hotelBooking;
    }

    public void Confirm()
    {
        if (Status != HotelBookingStatus.Pending)
            throw new InvalidOperationException(
                "Hotel booking can only be confirmed from pending state"
            );

        Status = HotelBookingStatus.Confirmed;
    }

    public void Cancel()
    {
        Status = HotelBookingStatus.Cancelled;
    }
}

public enum HotelBookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Failed,
}
