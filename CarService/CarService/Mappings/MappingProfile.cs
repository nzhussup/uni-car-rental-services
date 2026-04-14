using AutoMapper;
using CarRentalService.Data.Entities;
using CarService.Models.DTOs;
using CarService.Models.Responses;

namespace CarService.Mappings;

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
    }
}