namespace Car.API.Application.Handlers;

using AutoMapper;
using MediatR;
using Car.API.Application.Commands;
using Car.API.Application.DTOs;
using Car.API.Application.Queries;
using Car.API.Domain.Entities;
using Shared.Domain.Abstractions;

public class CreateCarRentalCommandHandler : IRequestHandler<CreateCarRentalCommand, CarRentalResponseDto>
{
    private readonly IRepository<CarRental> _repository;
    private readonly IMapper _mapper;

    public CreateCarRentalCommandHandler(IRepository<CarRental> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CarRentalResponseDto> Handle(CreateCarRentalCommand request, CancellationToken cancellationToken)
    {
        var carRental = CarRental.Create(
            request.UserId,
            request.CarModel,
            request.Company,
            request.PickUpDate,
            request.ReturnDate,
            request.PickUpLocation,
            request.PricePerDay);

        await _repository.AddAsync(carRental, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CarRentalResponseDto>(carRental);
    }
}

public class ConfirmCarRentalCommandHandler : IRequestHandler<ConfirmCarRentalCommand, CarRentalResponseDto>
{
    private readonly IRepository<CarRental> _repository;
    private readonly IMapper _mapper;

    public ConfirmCarRentalCommandHandler(IRepository<CarRental> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CarRentalResponseDto> Handle(ConfirmCarRentalCommand request, CancellationToken cancellationToken)
    {
        var carRental = await _repository.GetByIdAsync(request.CarRentalId, cancellationToken);
        if (carRental is null)
            throw new InvalidOperationException("Car rental not found");

        carRental.Confirm();
        await _repository.UpdateAsync(carRental, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CarRentalResponseDto>(carRental);
    }
}

public class CancelCarRentalCommandHandler : IRequestHandler<CancelCarRentalCommand, CarRentalResponseDto>
{
    private readonly IRepository<CarRental> _repository;
    private readonly IMapper _mapper;

    public CancelCarRentalCommandHandler(IRepository<CarRental> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CarRentalResponseDto> Handle(CancelCarRentalCommand request, CancellationToken cancellationToken)
    {
        var carRental = await _repository.GetByIdAsync(request.CarRentalId, cancellationToken);
        if (carRental is null)
            throw new InvalidOperationException("Car rental not found");

        carRental.Cancel();
        await _repository.UpdateAsync(carRental, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CarRentalResponseDto>(carRental);
    }
}

public class GetCarRentalByIdQueryHandler : IRequestHandler<GetCarRentalByIdQuery, CarRentalResponseDto>
{
    private readonly IRepository<CarRental> _repository;
    private readonly IMapper _mapper;

    public GetCarRentalByIdQueryHandler(IRepository<CarRental> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CarRentalResponseDto> Handle(GetCarRentalByIdQuery request, CancellationToken cancellationToken)
    {
        var carRental = await _repository.GetByIdAsync(request.CarRentalId, cancellationToken);
        if (carRental is null)
            throw new InvalidOperationException("Car rental not found");

        return _mapper.Map<CarRentalResponseDto>(carRental);
    }
}
