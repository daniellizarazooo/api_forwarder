using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Store;
using System.Text.Json;
using Models;
using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace Services
{
    public class ApiFetcher
    {
        private readonly Intensities _intensities;

        private readonly Scenes _scenes;

        private readonly HttpClient _httpClient = new HttpClient();

        private HttpClientHandler _handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        public ApiFetcher(Intensities intensities, Scenes scenes)
        {
            _intensities = intensities;
            _scenes = scenes;
            _httpClient = new HttpClient(_handler);
        }

        public async Task RunAsync2(CancellationToken cancellationToken, ILogger logger)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {

                    DateTime start = DateTime.Now;
                    // Console.WriteLine("\n");
                    // Console.WriteLine($"START: {start}");

                    var scenes = _scenes.GetAll();
                    var intensities = _intensities.GetAll();

                    var scenesLength = scenes.Count();
                    var intensitiesLength = intensities.Count();
                    int maxLength = 0;

                    if (scenesLength > intensitiesLength) maxLength = scenesLength;
                    else maxLength = intensitiesLength;


                    for (int i = 0; i < maxLength; i++)
                    {
                        if (scenesLength > 0 && i < scenesLength)
                        {
                            var scene = scenes.ElementAt(i);

                            try
                            {
                                string targetUrl = scene.Key;
                                string token = scene.Value.Token;

                                string sceneResponse = await ApiCall(targetUrl, token, cancellationToken);

                                var currentScene = JsonSerializer.Deserialize<Models.SceneResponse>(sceneResponse);
                                _scenes.updateValue(targetUrl, currentScene?.ActiveScene ?? -1);
                                
                            }
                            catch (Exception ex)
                            {

                                logger.LogError(ex, $"Error scene {scene.Value.Name}", ex.Message);
                                Console.WriteLine($"Error while calling {scene.Value.Name}: {ex.Message}");
                            }
                            await Task.Delay(500, cancellationToken);
                        }

                        if (intensitiesLength > 0 && i < intensitiesLength)
                        {
                            var intensity = intensities.ElementAt(i);
                            try
                            {
                                string targetUrl = intensity.Key;
                                string token = intensity.Value.Token;

                                string intensityResponse = await ApiCall(targetUrl, token, cancellationToken);
                                var currentIntensity = JsonSerializer.Deserialize<Models.LightingResponse>(intensityResponse);

                                _intensities.updateValue(targetUrl, currentIntensity?.Intensity ?? -1);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"Error scene {intensity.Value.Name}",ex.Message); 
                                Console.WriteLine($"Error while calling {intensity.Value.Name}: {ex.Message}");
                            }
                            await Task.Delay(500, cancellationToken);
                        }
                    }

                    TimeSpan diff = DateTime.Now - start;
                    // Console.WriteLine($"DateTime.Now: {DateTime.Now}");
                    // Console.WriteLine($"Elapsed seconds: {diff.TotalSeconds}");
                    
                    await Task.Delay(1000, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error task async"); 
                    Console.WriteLine($"Error RunAsync2 ${ex.Message}");
                }
            }
        }


        public async Task<string> ApiCall(string targetUrl, string token, CancellationToken cancellationToken) {
            var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent("");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return content;
        } 
    }
}