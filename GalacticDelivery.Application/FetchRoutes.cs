using GalacticDelivery.Common;
using GalacticDelivery.Domain;

namespace GalacticDelivery.Application;

public class FetchRoutes
{
    private readonly IRouteRepository _routeRepository;
    
    public FetchRoutes(IRouteRepository routeRepository)
    {
        _routeRepository = routeRepository;
    }

    public async Task<Result<IEnumerable<Guid>>> Execute()
    {
        var guids = await _routeRepository.FetchAll();
        return Result<IEnumerable<Guid>>.Success(guids);
    }
}