using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Models.DTOs;

namespace CarRentalService.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Car, CarDto>();
        CreateMap<CreateCarDto, Car>();
        CreateMap<UpdateCarDto, Car>();
    }
}