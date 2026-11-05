using AutoMapper;
using Firmeza.Application.DTOs;
using Firmeza.Domain.Entities;
namespace Firmeza.Application.Services;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<ProductInput, Product>(MemberList.None);
        CreateMap<Customer, CustomerDto>();
        CreateMap<CustomerInput, Customer>(MemberList.None);
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<VehicleInput, Vehicle>(MemberList.None);
        CreateMap<SaleDetail, SaleDetailDto>();
        CreateMap<Sale, SaleDto>().ForCtorParam("CustomerName", o => o.MapFrom(s => s.Customer.Name + " " + s.Customer.LastName)).ForCtorParam("Status", o => o.MapFrom(s => s.Status.ToString()));
        CreateMap<Rental, RentalDto>().ForCtorParam("VehicleName", o => o.MapFrom(r => r.Vehicle.Name)).ForCtorParam("Status", o => o.MapFrom(r => r.Status.ToString()));
    }
}
