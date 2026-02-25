namespace Booking.API.Domain.Entities;

using Shared.Domain.Abstractions;

/// <summary>
/// Booking aggregate root.
/// Represents a complete booking with flights, hotels, and cars.
/// </summary>
public class Booking : AggregateRoot
{
    public string ReferenceNumber { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public decimal TotalPrice { get; private set; }
    public DateTime CheckInDate { get; private set; }
    public DateTime CheckOutDate { get; private set; }
    public bool IncludeFlights { get; private set; }
    public bool IncludeHotel { get; private set; }
    public bool IncludeCar { get; private set; }

    public Guid? FlightBookingId { get; private set; }
    public Guid? HotelBookingId { get; private set; }
    public Guid? CarBookingId { get; private set; }

    private readonly List<BookingStep> _steps = [];
    public IReadOnlyList<BookingStep> Steps
    {
        get { return _steps.AsReadOnly(); }
    }

    public static Booking Create(
        Guid userId,
        DateTime checkInDate,
        DateTime checkOutDate,
        string referenceNumber,
        bool includeFlights,
        bool includeHotel,
        bool includeCar
    )
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            ReferenceNumber = referenceNumber,
            Status = BookingStatus.Pending,
            TotalPrice = 0,
            IncludeFlights = includeFlights,
            IncludeHotel = includeHotel,
            IncludeCar = includeCar,
        };
        return booking;
    }

    public void AddFlightBooking(Guid flightBookingId, decimal price)
    {
        FlightBookingId = flightBookingId;
        TotalPrice += price;
        _steps.Add(
            new BookingStep
            {
                StepType = SagaStepType.FlightBooking,
                Status = SagaStepStatus.Completed,
                ExternalId = flightBookingId,
            }
        );
    }

    public void AddHotelBooking(Guid hotelBookingId, decimal price)
    {
        HotelBookingId = hotelBookingId;
        TotalPrice += price;
        _steps.Add(
            new BookingStep
            {
                StepType = SagaStepType.HotelBooking,
                Status = SagaStepStatus.Completed,
                ExternalId = hotelBookingId,
            }
        );
    }

    public void AddCarBooking(Guid carBookingId, decimal price)
    {
        CarBookingId = carBookingId;
        TotalPrice += price;
        _steps.Add(
            new BookingStep
            {
                StepType = SagaStepType.CarBooking,
                Status = SagaStepStatus.Completed,
                ExternalId = carBookingId,
            }
        );
    }

    public void MarkAsProcessing()
    {
        if (Status == BookingStatus.Pending)
        {
            Status = BookingStatus.Processing;
        }
    }

    public void MarkAsConfirmed()
    {
        Status = BookingStatus.Confirmed;
    }

    public void MarkAsFailed(string reason)
    {
        Status = BookingStatus.Failed;
    }

    public void UpdateStepStatus(SagaStepType stepType, SagaStepStatus stepStatus)
    {
        var step = _steps.FirstOrDefault(s => s.StepType == stepType);
        if (step is not null)
        {
            step.Status = stepStatus;
        }
    }

    public void MarkStepFailed(SagaStepType stepType, string reason)
    {
        var step = _steps.FirstOrDefault(s => s.StepType == stepType);
        if (step is null)
        {
            _steps.Add(
                new BookingStep
                {
                    StepType = stepType,
                    Status = SagaStepStatus.Failed,
                    ExternalId = Guid.Empty,
                    ErrorMessage = reason,
                }
            );
            return;
        }

        step.Status = SagaStepStatus.Failed;
        step.ErrorMessage = reason;
    }

    public bool IsReadyToConfirm()
    {
        return (!IncludeFlights || FlightBookingId.HasValue)
            && (!IncludeHotel || HotelBookingId.HasValue)
            && (!IncludeCar || CarBookingId.HasValue);
    }
}

public enum BookingStatus
{
    Pending,
    Processing,
    Confirmed,
    Failed,
    Cancelled,
}

public class BookingStep
{
    public int Id { get; set; }
    public Guid BookingId { get; set; }
    public SagaStepType StepType { get; set; }
    public SagaStepStatus Status { get; set; }
    public Guid ExternalId { get; set; }
    public string? ErrorMessage { get; set; }

    public Booking? Booking { get; set; }
}

public enum SagaStepType
{
    FlightBooking,
    HotelBooking,
    CarBooking,
}

public enum SagaStepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Compensated,
}
