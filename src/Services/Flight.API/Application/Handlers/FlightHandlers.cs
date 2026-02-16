namespace Flight.API.Application.Handlers;

using AutoMapper;
using MediatR;
using Flight.API.Application.Commands;
using Flight.API.Application.DTOs;
using Flight.API.Application.Queries;
using Flight.API.Domain.Entities;
using Shared.Domain.Abstractions;

public class CreateFlightBookingCommandHandler : IRequestHandler<CreateFlightBookingCommand, FlightBookingResponseDto>
{
    private readonly IRepository<FlightBooking> _repository;
    private readonly IMapper _mapper;

    public CreateFlightBookingCommandHandler(IRepository<FlightBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<FlightBookingResponseDto> Handle(CreateFlightBookingCommand request, CancellationToken cancellationToken)
    {
        var flightBooking = FlightBooking.Create(
            request.UserId,
            request.DepartureCity,
            request.ArrivalCity,
            request.DepartureDateUtc,
            request.ArrivalDateUtc,
            request.Price,
            request.PassengerCount);

        await _repository.AddAsync(flightBooking, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FlightBookingResponseDto>(flightBooking);
    }
}

public class ConfirmFlightBookingCommandHandler : IRequestHandler<ConfirmFlightBookingCommand, FlightBookingResponseDto>
{
    private readonly IRepository<FlightBooking> _repository;
    private readonly IMapper _mapper;

    public ConfirmFlightBookingCommandHandler(IRepository<FlightBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<FlightBookingResponseDto> Handle(ConfirmFlightBookingCommand request, CancellationToken cancellationToken)
    {
        var flightBooking = await _repository.GetByIdAsync(request.FlightBookingId, cancellationToken);
        if (flightBooking is null)
            throw new InvalidOperationException("Flight booking not found");

        flightBooking.Confirm();
        await _repository.UpdateAsync(flightBooking, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FlightBookingResponseDto>(flightBooking);
    }
}

public class CancelFlightBookingCommandHandler : IRequestHandler<CancelFlightBookingCommand, FlightBookingResponseDto>
{
    private readonly IRepository<FlightBooking> _repository;
    private readonly IMapper _mapper;

    public CancelFlightBookingCommandHandler(IRepository<FlightBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<FlightBookingResponseDto> Handle(CancelFlightBookingCommand request, CancellationToken cancellationToken)
    {
        var flightBooking = await _repository.GetByIdAsync(request.FlightBookingId, cancellationToken);
        if (flightBooking is null)
            throw new InvalidOperationException("Flight booking not found");

        flightBooking.Cancel();
        await _repository.UpdateAsync(flightBooking, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<FlightBookingResponseDto>(flightBooking);
    }
}

public class GetFlightBookingByIdQueryHandler : IRequestHandler<GetFlightBookingByIdQuery, FlightBookingResponseDto>
{
    private readonly IRepository<FlightBooking> _repository;
    private readonly IMapper _mapper;

    public GetFlightBookingByIdQueryHandler(IRepository<FlightBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<FlightBookingResponseDto> Handle(GetFlightBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var flightBooking = await _repository.GetByIdAsync(request.FlightBookingId, cancellationToken);
        if (flightBooking is null)
            throw new InvalidOperationException("Flight booking not found");

        return _mapper.Map<FlightBookingResponseDto>(flightBooking);
    }
}
