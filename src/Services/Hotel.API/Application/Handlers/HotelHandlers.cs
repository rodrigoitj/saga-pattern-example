namespace Hotel.API.Application.Handlers;

using AutoMapper;
using MediatR;
using Hotel.API.Application.Commands;
using Hotel.API.Application.DTOs;
using Hotel.API.Application.Queries;
using Hotel.API.Domain.Entities;
using Shared.Domain.Abstractions;

public class CreateHotelBookingCommandHandler : IRequestHandler<CreateHotelBookingCommand, HotelBookingResponseDto>
{
    private readonly IRepository<HotelBooking> _repository;
    private readonly IMapper _mapper;

    public CreateHotelBookingCommandHandler(IRepository<HotelBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<HotelBookingResponseDto> Handle(CreateHotelBookingCommand request, CancellationToken cancellationToken)
    {
        var hotelBooking = HotelBooking.Create(
            request.UserId,
            request.HotelName,
            request.City,
            request.CheckInDate,
            request.CheckOutDate,
            request.RoomCount,
            request.PricePerNight);

        await _repository.AddAsync(hotelBooking, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<HotelBookingResponseDto>(hotelBooking);
    }
}

public class ConfirmHotelBookingCommandHandler : IRequestHandler<ConfirmHotelBookingCommand, HotelBookingResponseDto>
{
    private readonly IRepository<HotelBooking> _repository;
    private readonly IMapper _mapper;

    public ConfirmHotelBookingCommandHandler(IRepository<HotelBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<HotelBookingResponseDto> Handle(ConfirmHotelBookingCommand request, CancellationToken cancellationToken)
    {
        var hotelBooking = await _repository.GetByIdAsync(request.HotelBookingId, cancellationToken);
        if (hotelBooking is null)
            throw new InvalidOperationException("Hotel booking not found");

        hotelBooking.Confirm();
        await _repository.UpdateAsync(hotelBooking, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<HotelBookingResponseDto>(hotelBooking);
    }
}

public class CancelHotelBookingCommandHandler : IRequestHandler<CancelHotelBookingCommand, HotelBookingResponseDto>
{
    private readonly IRepository<HotelBooking> _repository;
    private readonly IMapper _mapper;

    public CancelHotelBookingCommandHandler(IRepository<HotelBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<HotelBookingResponseDto> Handle(CancelHotelBookingCommand request, CancellationToken cancellationToken)
    {
        var hotelBooking = await _repository.GetByIdAsync(request.HotelBookingId, cancellationToken);
        if (hotelBooking is null)
            throw new InvalidOperationException("Hotel booking not found");

        hotelBooking.Cancel();
        await _repository.UpdateAsync(hotelBooking, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<HotelBookingResponseDto>(hotelBooking);
    }
}

public class GetHotelBookingByIdQueryHandler : IRequestHandler<GetHotelBookingByIdQuery, HotelBookingResponseDto>
{
    private readonly IRepository<HotelBooking> _repository;
    private readonly IMapper _mapper;

    public GetHotelBookingByIdQueryHandler(IRepository<HotelBooking> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<HotelBookingResponseDto> Handle(GetHotelBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var hotelBooking = await _repository.GetByIdAsync(request.HotelBookingId, cancellationToken);
        if (hotelBooking is null)
            throw new InvalidOperationException("Hotel booking not found");

        return _mapper.Map<HotelBookingResponseDto>(hotelBooking);
    }
}
