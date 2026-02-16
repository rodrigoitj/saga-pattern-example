namespace Booking.API.Application.Mappings;

using AutoMapper;
using Booking.API.Application.DTOs;
using Booking.API.Domain.Entities;

public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        CreateMap<Booking, BookingResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<BookingStep, BookingStepDto>()
            .ForMember(dest => dest.StepType, opt => opt.MapFrom(src => src.StepType.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
