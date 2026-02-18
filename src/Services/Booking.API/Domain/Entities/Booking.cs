namespace Booking.API.Domain.Entities;

using global::Booking.API.Domain.Events;
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
    
    public Guid? FlightBookingId { get; private set; }
    public Guid? HotelBookingId { get; private set; }
    public Guid? CarBookingId { get; private set; }

    private readonly List<BookingStep> _steps = [];
    public IReadOnlyList<BookingStep> Steps => _steps.AsReadOnly();

    public static Booking Create(
        Guid userId,
        DateTime checkInDate,
        DateTime checkOutDate,
        string referenceNumber)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            ReferenceNumber = referenceNumber,
            Status = BookingStatus.Pending,
            TotalPrice = 0
        };

        booking.RaiseDomainEvent(new BookingCreatedEvent 
        { 
            AggregateId = booking.Id,
            BookingId = booking.Id,
            UserId = userId,
            ReferenceNumber = referenceNumber
        });

        return booking;
    }

    public void AddFlightBooking(Guid flightBookingId, decimal price)
    {
        FlightBookingId = flightBookingId;
        TotalPrice += price;
        _steps.Add(new BookingStep 
        { 
            StepType = SagaStepType.FlightBooking, 
            Status = SagaStepStatus.Pending,
            ExternalId = flightBookingId
        });
    }

    public void AddHotelBooking(Guid hotelBookingId, decimal price)
    {
        HotelBookingId = hotelBookingId;
        TotalPrice += price;
        _steps.Add(new BookingStep 
        { 
            StepType = SagaStepType.HotelBooking, 
            Status = SagaStepStatus.Pending,
            ExternalId = hotelBookingId
        });
    }

    public void AddCarBooking(Guid carBookingId, decimal price)
    {
        CarBookingId = carBookingId;
        TotalPrice += price;
        _steps.Add(new BookingStep 
        { 
            StepType = SagaStepType.CarBooking, 
            Status = SagaStepStatus.Pending,
            ExternalId = carBookingId
        });
    }

    public void MarkAsConfirmed()
    {
        Status = BookingStatus.Confirmed;
        RaiseDomainEvent(new BookingConfirmedEvent 
        { 
            AggregateId = Id,
            BookingId = Id
        });
    }

    public void MarkAsFailed(string reason)
    {
        Status = BookingStatus.Failed;
        RaiseDomainEvent(new BookingFailedEvent 
        { 
            AggregateId = Id,
            BookingId = Id,
            Reason = reason
        });
    }

    public void UpdateStepStatus(SagaStepType stepType, SagaStepStatus stepStatus)
    {
        var step = _steps.FirstOrDefault(s => s.StepType == stepType);
        if (step is not null)
        {
            step.Status = stepStatus;
        }
    }
}

public enum BookingStatus
{
    Pending,
    Processing,
    Confirmed,
    Failed,
    Cancelled
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
    CarBooking
}

public enum SagaStepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Compensated
}
