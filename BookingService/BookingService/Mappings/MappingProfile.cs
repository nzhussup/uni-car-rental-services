using AutoMapper;
using BookingService.Models.DTOs;
using BookingService.Models.Responses;
using CarRentalService.Data.Entities;
using Keycloak.AuthServices.Sdk.Admin.Models;

namespace BookingService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
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