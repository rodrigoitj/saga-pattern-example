namespace Flight.API.Application.Mappings;

using AutoMapper;
using Flight.API.Application.DTOs;
using Flight.API.Domain.Entities;

public class FlightMappingProfile : Profile
{
    public FlightMappingProfile()
    {
        CreateMap<FlightBooking, FlightBookingResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
