using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Models.DTOs;
using Keycloak.AuthServices.Sdk.Admin.Models;

namespace CarRentalService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Car, CarDto>();
        CreateMap<CreateCarDto, Car>();
        CreateMap<UpdateCarDto, Car>();
        CreateMap<Booking, BookingDto>();
        CreateMap<CreateBookingDto, Booking>();
        CreateMap<UserRepresentation, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)));
    }
}