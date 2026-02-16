namespace Hotel.API.Application.Mappings;

using AutoMapper;
using Hotel.API.Application.DTOs;
using Hotel.API.Domain.Entities;

public class HotelMappingProfile : Profile
{
    public HotelMappingProfile()
    {
        CreateMap<HotelBooking, HotelBookingResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
