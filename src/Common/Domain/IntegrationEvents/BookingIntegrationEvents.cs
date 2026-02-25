namespace Shared.Domain.IntegrationEvents;

/// <summary>
/// Represents the type of booking step in a booking process.
/// </summary>
public enum BookingStepType
{
    /// <summary>Represents a flight booking step.</summary>
    Flight,

    /// <summary>Represents a hotel booking step.</summary>
    Hotel,

    /// <summary>Represents a car rental booking step.</summary>
    Car,
}

/// <summary>
/// Integration event raised when a booking is created.
/// </summary>
public record BookingCreatedIntegrationEvent
{
    /// <summary>Gets the unique identifier for the booking.</summary>
    public Guid BookingId { get; init; }

    /// <summary>Gets the unique identifier for the user.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets the check-in date for the booking.</summary>
    public DateTime CheckInDate { get; init; }

    /// <summary>Gets the check-out date for the booking.</summary>
    public DateTime CheckOutDate { get; init; }

    /// <summary>Gets a value indicating whether flights are included.</summary>
    public bool IncludeFlights { get; init; }

    /// <summary>Gets a value indicating whether hotel is included.</summary>
    public bool IncludeHotel { get; init; }

    /// <summary>Gets a value indicating whether car rental is included.</summary>
    public bool IncludeCar { get; init; }

    /// <summary>Gets the reference number for the booking.</summary>
    public string ReferenceNumber { get; init; } = string.Empty;
}

/// <summary>
/// Integration event raised when a booking step is completed.
/// </summary>
public record BookingStepCompletedIntegrationEvent
{
    /// <summary>Gets the unique identifier for the booking.</summary>
    public Guid BookingId { get; init; }

    /// <summary>Gets the type of booking step completed.</summary>
    public BookingStepType StepType { get; init; }

    /// <summary>Gets the external identifier for the step.</summary>
    public Guid ExternalId { get; init; }

    /// <summary>Gets the price for the booking step.</summary>
    public decimal Price { get; init; }

    /// <summary>Gets the confirmation code for the booking step.</summary>
    public string ConfirmationCode { get; init; } = string.Empty;
}

/// <summary>
/// Integration event raised when a booking fails.
/// </summary>
public record BookingFailedIntegrationEvent
{
    /// <summary>Gets the unique identifier for the booking.</summary>
    public Guid BookingId { get; init; }

    /// <summary>Gets the type of booking step that failed.</summary>
    public BookingStepType StepType { get; init; }

    /// <summary>Gets the reason for the booking failure.</summary>
    public string Reason { get; init; } = string.Empty;
}
