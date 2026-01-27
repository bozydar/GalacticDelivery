using System.Net.Http;
using NBomber.CSharp;
using NBomber.Http.CSharp;

var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5114") };

var scenario = Scenario.Create("get_free_drivers", async context =>
    {
        var request = Http.CreateRequest("GET", "/api/drivers/free");
        var response = await Http.Send(httpClient, request);

        return response;
    })
    .WithLoadSimulations(
        Simulation.RampingConstant(copies: 20, during: TimeSpan.FromSeconds(30)),
        Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(30))
    );

NBomberRunner
    .RegisterScenarios(scenario)
    .Run();