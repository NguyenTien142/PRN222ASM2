using AutoMapper;
using Repositories.Model;
using Services.DataTransferObject.UserDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpper.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // User mappings
            CreateMap<User, GetUserRespond>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName))
                .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.Dealer, opt => opt.MapFrom(src => src.Dealer));

            // Customer mappings
            CreateMap<Customer, CustomerInfo>();

            // Dealer mappings
            CreateMap<Dealer, DealerInfo>();

            // Register request to User
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

            // Register request to Customer
            CreateMap<RegisterRequest, Customer>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CustomerName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.CustomerPhone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.CustomerEmail))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.CustomerAddress));

            // Register request to Dealer
            CreateMap<RegisterRequest, Dealer>()
                .ForMember(dest => dest.DealerName, opt => opt.MapFrom(src => src.DealerName))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.DealerAddress))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.DealerQuantity ?? 0));
        }
    }
}
