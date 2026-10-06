using AutoMapper;
using Firmeza.Domain.Entities;
using Firmeza.Application.Products;
using Firmeza.Application.Customers;
using Firmeza.Application.Vehicles;
using Firmeza.Application.Sales;
using Firmeza.Application.Rentals;
namespace Firmeza.Application.Common;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductResponse>();
        CreateMap<Customer, CustomerResponse>();
        CreateMap<Vehicle, VehicleResponse>();
        CreateMap<SaleItem, SaleItemResponse>();
        CreateMap<Sale, SaleResponse>().ForCtorParam("Status", o => o.MapFrom(s => s.Status.ToString()));
        CreateMap<Rental, RentalResponse>().ForCtorParam("Status", o => o.MapFrom(s => s.Status.ToString()));
    }
}
