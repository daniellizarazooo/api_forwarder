using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Store;
using System.Text.Json;

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

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {

                try
                {
                    // Loop scenes
                    if (!_scenes.GetAll().IsEmpty)
                    {
                        foreach (var item in _scenes.GetAll())
                        {
                            try
                            {
                                string targetUrl = item.Key;
                                Models.Data data = item.Value;

                                var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", data.Token);
                                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                request.Content = new StringContent("");

                                var response = await _httpClient.SendAsync(request, cancellationToken);
                                response.EnsureSuccessStatusCode();

                                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                                var scene = JsonSerializer.Deserialize<Models.SceneResponse>(content);
                                // Console.WriteLine($"TASK targetUrl = {targetUrl} Scene value: {scene?.ActiveScene ?? -1}");
                                _scenes.updateValue(targetUrl, scene?.ActiveScene ?? -1);
                                await Task.Delay(600, cancellationToken);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error _scenes looping{e.Message}");
                            }
                        }
                    }
                    await Task.Delay(500, cancellationToken);
                    // Loop values in Store
                    if (!_intensities.GetAll().IsEmpty)
                    {
                        foreach (var item in _intensities.GetAll())
                        {
                            try
                            {

                                string targetUrl = item.Key;
                                Models.Data data = item.Value;
                                var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", data.Token);
                                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                request.Content = new StringContent("");

                                var response = await _httpClient.SendAsync(request, cancellationToken);
                                response.EnsureSuccessStatusCode();

                                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                                var lighting = JsonSerializer.Deserialize<Models.LightingResponse>(content);

                                // Console.WriteLine($"TASK targetUrl = {targetUrl} Lighting value: {lighting?.Intensity ?? -1}");
                                // Console.WriteLine($"\n");
                                var count = _intensities.GetAll().Count();
                                // Console.WriteLine($"count {count}");
                                // Console.WriteLine($"\n");
                                _intensities.updateValue(targetUrl, lighting?.Intensity ?? -1);
                                await Task.Delay(600, cancellationToken);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error {e.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching API: {ex.Message}");
                }

                await Task.Delay(3000, cancellationToken);
            }
        }
    }
}