namespace Car.API.Application.Mappings;

using AutoMapper;
using Car.API.Application.DTOs;
using Car.API.Domain.Entities;

public class CarMappingProfile : Profile
{
    public CarMappingProfile()
    {
        CreateMap<CarRental, CarRentalResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
