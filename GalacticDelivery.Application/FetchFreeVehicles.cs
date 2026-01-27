using GalacticDelivery.Common;
using GalacticDelivery.Domain;

namespace GalacticDelivery.Application;

public class FetchFreeVehicles
{
    private readonly IVehicleRepository _vehicleRepository;
    
    public FetchFreeVehicles(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<IEnumerable<Guid>>> Execute()
    {
        var guids = await _vehicleRepository.FetchAllFree();
        return Result<IEnumerable<Guid>>.Success(guids);
    }
}