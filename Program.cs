using System.Collections.Concurrent;
using System.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services;
using Store;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<Intensities>();
builder.Services.AddSingleton<Scenes>();


builder.WebHost.UseUrls("http://*:5005");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
    });
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .WriteTo.File("Logs/errors.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// App information

app.Logger.LogInformation("Api Forwarding");
app.Logger.LogInformation("Running in port 5004");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

app.UseSwagger();
app.UseSwaggerUI();

//DATA STORE 
var intensities = app.Services.GetRequiredService<Intensities>();
var scenes = app.Services.GetRequiredService<Scenes>();

// RUN TASKS
var cts = new CancellationTokenSource();
var fetcher = new ApiFetcher(intensities,scenes);
_ = Task.Run(() => fetcher.RunAsync2(cts.Token, app.Logger));

// ------------------------------------------ROUTES -----------------------------------------------------
app.MapGet("/", () =>
{
    return Results.Ok("SERVER RUNNING");
}).WithName("Test")
.WithTags("Test")
.WithOpenApi();

app.MapGet("/error", () =>
{
    Log.Error($"I CAN DO EVERYTHING THROUGH CHRIST WHO STRENGTHENS ME");
})
.WithName("Error")
.WithTags("Test")
.WithOpenApi();

app.MapGet("/proxy/light/all", () =>
{

    var allIntensities = intensities.GetAll();

    return Results.Ok(allIntensities);
})
.WithName("GetAllIntensities")
.WithTags("Light")
.WithOpenApi();

app.MapGet("proxy/scene/all", ([FromQuery] bool? on) =>
{

    // on queryparameter is a filter condition to return only Scenes different than 0.
    var allScenes = scenes.GetAll();

    if (on == true)
    {
        var filtered = allScenes
            .Where(kvp => kvp.Value.Value != 0)
            .Select(kvp => new { kvp.Value.Name, kvp.Value.Value })
            .ToList();

        return Results.Ok(filtered);

    }

    var result = allScenes
    .Select(kvp => new { kvp.Value.Name, kvp.Value.Value });

    return Results.Ok(result);
})
.WithName("GetAllScenes")
.WithTags("Scene")
.WithOpenApi();

/*
* /proxy/light returns the lighting intesity for a given url
*/
app.MapGet("/proxy/light",  ([FromQuery] string? url,[FromQuery] string? token, [FromQuery] string? name) =>
{
    if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token))
        return Results.Ok(-1);

    string decodedUrl = Uri.UnescapeDataString(url);

    if (!decodedUrl.Contains("lighting"))
    {
        decodedUrl += "/lighting";
    }

    string decodedToken = Uri.UnescapeDataString(token);

    bool keyExists = intensities.KeyExists(decodedUrl);

    if (!keyExists)
    {
        intensities.Add(decodedUrl, decodedToken,name);
        return Results.Ok(-1);
    }

    double? intensity = intensities.GetByKey(decodedUrl);

    if (intensity == null) return Results.Ok(-1);
    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: /lighting name:{name} intensity:{intensity ?? -1}");

    return Results.Ok(intensity);

})
.WithName("GetLitghtingIntensity")
.WithTags("Light")
.WithOpenApi();

app.MapGet("/proxy/scene", ([FromQuery] string url, [FromQuery] string token,[FromQuery] string? name) =>
{

    if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token))
        return Results.Ok(-1);

    string decodedUrl = Uri.UnescapeDataString(url);

    if (!decodedUrl.Contains("scene"))
    {
        decodedUrl += "/scene";
    }

    string decodedToken = Uri.UnescapeDataString(token);

    bool keyExists = scenes.KeyExists(decodedUrl);

    if (!keyExists)
    {
        scenes.Add(decodedUrl, decodedToken,name);
        return Results.Ok(-1);
    }

    double? scene = scenes.GetByKey(decodedUrl);

    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: /scene name:{name} scene:{scene ?? -1}");

    return Results.Ok(scene ?? -1);
}
)
.WithName("GetScene")
.WithTags("Scene")
.WithOpenApi();

app.MapPost("/proxy/scene", async (SceneToSet payload, [FromQuery] string? name) =>
{

    string targetUrl = Uri.UnescapeDataString(payload.Url); // Url of device to change scene

    if (!targetUrl.Contains("/scene"))
    {
        targetUrl += "/scene";
    }

    string token = Uri.UnescapeDataString(payload.Token); // Token for Tridonic devices
    byte sceneToBeSet = payload.Scene;

    using var handler = new HttpClientHandler
    {
        // Ignore SSL certificate issues (like verify=False in Python)
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    using var client = new HttpClient(handler);

    // Set headers
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    // Prepare JSON body
    var jsonContent = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(new { activeScene = sceneToBeSet }),
        System.Text.Encoding.UTF8,
        "application/json"
    );

    Console.WriteLine($"\n{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} Request: Scene to set {name}: {sceneToBeSet}\n");

    try
    {
        // Make PUT request
        var response = await client.PutAsync(targetUrl, jsonContent);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        SceneResponse? jsonResponse = System.Text.Json.JsonSerializer.Deserialize<SceneResponse>(responseBody);

        // Console.WriteLine($"Active scene response: {jsonResponse?.ActiveScene}");

        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: /put/scene scene:{jsonResponse?.ActiveScene}");

        return Results.Ok(jsonResponse?.ActiveScene ?? -1);
    }
    catch (Exception ex)
    {
        Log.Error(ex, $"Error when trying to update {name}",ex.Message);
        Console.WriteLine($"Error during PUT: {ex.Message}");
        return Results.Ok(-1);
    }
})
.WithName("ChangeScene")
.WithTags("Scene")
.WithOpenApi();

app.UseCors();
app.Run();
