using GalacticDelivery.Api.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddOpenApi();
builder.ConfigureLogging();
builder.ConfigureDatabaseConnection();
builder.RegisterServices();
builder.ConfigureJsonSerializationOptions();

var app = builder.Build();
app.SetupOpenApi();
app.InitializeDatabaseSchema();
app.SetExceptionSupport();
app.ConfigureApiEndpoints();
app.Run();
