using System.Text.Json.Serialization;

namespace GalacticDelivery.Api.Web.Extensions;

public static class Json
{
    public static void ConfigureJsonSerializationOptions(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter()
            );
        });
    }

    public static void AddOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
    }

    public static void SetupOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
    }
}
