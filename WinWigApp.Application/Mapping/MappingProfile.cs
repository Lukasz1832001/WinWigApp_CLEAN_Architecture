using AutoMapper;
using WinWigApp.Application.DTOs;
using WinWigApp.Domain.Entities;

namespace WinWigApp.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserResponse>();

        // Strategy mappings
        CreateMap<Strategy, StrategyResponse>();

        // Deposit mappings
        CreateMap<Deposit, DepositsResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

        CreateMap<Deposit, DepositResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

        // Portfolio mappings
        CreateMap<Portfolio, PortfolioItemResponse>();
    }
}
