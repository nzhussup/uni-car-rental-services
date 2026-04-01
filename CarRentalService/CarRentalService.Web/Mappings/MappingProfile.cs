using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Models.DTOs;
using CarRentalService.Models.Responses;
using Keycloak.AuthServices.Sdk.Admin.Models;

namespace CarRentalService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Car, CarDto>();
        CreateMap<CarDto, CarResponse>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => new PriceDto()
            {
                Amount = src.PriceInUsd,
                Currency = "USD"
            }));
        CreateMap<QueryResponse<CarDto>, QueryResponse<CarResponse>>();
        CreateMap<CreateCarDto, Car>();
        CreateMap<UpdateCarDto, Car>();
        CreateMap<Booking, BookingDto>();
        CreateMap<BookingDto, BookingResponse>()
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => new PriceDto()
            {
                Amount = src.TotalCostInUsd,
                Currency = "USD"
            }));
        CreateMap<QueryResponse<BookingDto>, QueryResponse<BookingResponse>>();
        CreateMap<CreateBookingDto, Booking>();
        CreateMap<UserRepresentation, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)));
    }
}