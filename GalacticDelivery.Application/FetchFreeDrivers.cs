using GalacticDelivery.Common;
using GalacticDelivery.Domain;

namespace GalacticDelivery.Application;

public class FetchFreeDrivers
{
    private readonly IDriverRepository _driverRepository;
    
    public FetchFreeDrivers(IDriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<Result<IEnumerable<Guid>>> Execute()
    {
        var guids = await _driverRepository.FetchAllFree();
        return Result<IEnumerable<Guid>>.Success(guids);
    }
}