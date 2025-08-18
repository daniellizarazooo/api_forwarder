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

builder.WebHost.UseUrls("http://*:5004");

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global variables to use. If server restarted they restart
ConcurrentDictionary<string, Data> lightingDict = new ConcurrentDictionary<string, Data>();

//DATA STORE 

var intensities = app.Services.GetRequiredService<Intensities>();
var scenes = app.Services.GetRequiredService<Scenes>();
// Run tasks
var cts = new CancellationTokenSource();
var fetcher = new ApiFetcher(intensities,scenes);
_ = Task.Run(() => fetcher.RunAsync(cts.Token));


app.MapGet("/", () =>
{
    return Results.Ok("SERVER RUNNING");
}).WithName("Test")
.WithOpenApi();

app.MapGet("/proxy/light",  ([FromQuery] string? url,[FromQuery] string? token) =>
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
        intensities.Add(decodedUrl, decodedToken);
        return Results.Ok(-1);
    }

    double? intensity = intensities.GetByKey(decodedUrl);

    if (intensity == null) return Results.Ok(-1);

    return Results.Ok(intensity);

})
.WithName("GetLitghtingIntensity")
.WithOpenApi();

app.MapGet("/proxy/scene", ([FromQuery] string url, [FromQuery] string token) =>
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
        scenes.Add(decodedUrl, decodedToken);
        return Results.Ok(-1);
    }

    double? scene = scenes.GetByKey(decodedUrl);

    if (scene == null) return Results.Ok(-1);

    return Results.Ok(scene);
}
)
.WithName("GetScene")
.WithOpenApi();

app.MapPost("/proxy/scene", async (SceneToSet payload) =>
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

    try
    {
        // Make PUT request
        var response = await client.PutAsync(targetUrl, jsonContent);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        SceneResponse? jsonResponse = System.Text.Json.JsonSerializer.Deserialize<SceneResponse>(responseBody);

        // Console.WriteLine($"Active scene response: {jsonResponse?.ActiveScene}");

        return Results.Ok(jsonResponse?.ActiveScene ?? -1);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during PUT: {ex.Message}");
        return Results.Ok(-1);
    }
})
.WithName("ChangeScene")
.WithOpenApi();

app.UseCors();
app.Run();
