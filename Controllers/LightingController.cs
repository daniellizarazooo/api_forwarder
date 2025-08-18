// using Microsoft.AspNetCore.Mvc;
// using Models;

// namespace api_forwarder.Controllers;

// [ApiController]
// [Route("[controller]")]
// public class LightingController : ControllerBase
// {
//     private static List<Scene> scenes = new List<Scene>();
//     private static List<Intensity> intensities = new List<Intensity>();
//     private readonly IHttpClientFactory _clientFactory;

//     public LightingController(IHttpClientFactory clientFactory)
//     {
//         _clientFactory = clientFactory;
//     }

//     [HttpPost("intensity")]
//     public async Task<IActionResult> PostIntensity(Intensity intensity)
//     {
//         var client = _clientFactory.CreateClient();
//         var response = await client.PostAsJsonAsync(intensity.Url, new { intensity = intensity.Value });
//         intensities.Add(intensity);
//         return Ok(response);
//     }

//     [HttpPost("scene")]
//     public async Task<IActionResult> PostScene(Scene scene)
//     {
//         var client = _clientFactory.CreateClient();
//         var response = await client.PostAsJsonAsync(scene.Url, new { scene = scene.Value });
//         scenes.Add(scene);
//         return Ok(response);
//     }

//     [HttpGet("intensity")]
//     public IActionResult GetIntensity()
//     {
//         return Ok(intensities);
//     }

//     [HttpGet("scene")]
//     public IActionResult GetScene()
//     {
//         return Ok(scenes);
//     }
// }
