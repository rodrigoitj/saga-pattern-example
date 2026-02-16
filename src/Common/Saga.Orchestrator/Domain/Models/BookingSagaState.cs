namespace Saga.Orchestrator.Domain.Models;

/// <summary>
/// Saga State Machine for managing booking saga orchestration.
/// </summary>
public class BookingSagaState
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public SagaStatus Status { get; set; } = SagaStatus.Started;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Booking request details
    public bool IncludeFlights { get; set; }
    public bool IncludeHotel { get; set; }
    public bool IncludeCar { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    // Step completion tracking
    public StepStatus FlightBookingStatus { get; set; } = StepStatus.Pending;
    public Guid? FlightBookingId { get; set; }
    public string? FlightConfirmationCode { get; set; }

    public StepStatus HotelBookingStatus { get; set; } = StepStatus.Pending;
    public Guid? HotelBookingId { get; set; }
    public string? HotelConfirmationCode { get; set; }

    public StepStatus CarBookingStatus { get; set; } = StepStatus.Pending;
    public Guid? CarBookingId { get; set; }
    public string? CarReservationCode { get; set; }

    // Compensation tracking
    public string? FailureReason { get; set; }
    public List<string> CompensatedSteps { get; set; } = [];

    public bool IsSuccessful() =>
        Status == SagaStatus.Completed &&
        (!IncludeFlights || FlightBookingStatus == StepStatus.Completed) &&
        (!IncludeHotel || HotelBookingStatus == StepStatus.Completed) &&
        (!IncludeCar || CarBookingStatus == StepStatus.Completed);

    public IEnumerable<(StepType Step, StepStatus Status, Guid? Id)> GetPendingSteps()
    {
        if (IncludeFlights && FlightBookingStatus == StepStatus.Pending)
            yield return (StepType.Flight, FlightBookingStatus, FlightBookingId);

        if (IncludeHotel && HotelBookingStatus == StepStatus.Pending)
            yield return (StepType.Hotel, HotelBookingStatus, HotelBookingId);

        if (IncludeCar && CarBookingStatus == StepStatus.Pending)
            yield return (StepType.Car, CarBookingStatus, CarBookingId);
    }
}

public enum SagaStatus
{
    Started,
    BookingFlights,
    BookingHotel,
    BookingCar,
    Compensating,
    Completed,
    Failed
}

public enum StepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Compensated
}

public enum StepType
{
    Flight,
    Hotel,
    Car
}
