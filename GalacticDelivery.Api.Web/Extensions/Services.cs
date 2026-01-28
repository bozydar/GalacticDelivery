using GalacticDelivery.Application;
using GalacticDelivery.Application.Reports;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;

namespace GalacticDelivery.Api.Web.Extensions;

public static class Services
{
    public static void RegisterServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IDriverRepository, SqliteDriverRepository>();
        builder.Services.AddScoped<IVehicleRepository, SqliteVehicleRepository>();
        builder.Services.AddScoped<IRouteRepository, SqliteRouteRepository>();
        builder.Services.AddScoped<ITripRepository, SqliteTripRepository>();
        builder.Services.AddScoped<ITransactionManager, SqliteTransactionManager>();
        builder.Services.AddScoped<ITripReportRepository, SqliteTripReportRepository>();
        builder.Services.AddScoped<ITripReportProjection, TripReportProjection>();
        builder.Services.AddScoped<FetchFreeDrivers>();
        builder.Services.AddScoped<FetchFreeVehicles>();
        builder.Services.AddScoped<FetchRoutes>();
        builder.Services.AddScoped<GetTripReport>();
        builder.Services.AddScoped<PlanTrip>();
        builder.Services.AddScoped<ProcessEvent>();
    }
}
