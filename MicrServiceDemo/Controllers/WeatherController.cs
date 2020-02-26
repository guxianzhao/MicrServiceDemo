using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MicrServiceDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet]
        public string Get()
        {
            var summary = $"天气：{Summaries[new Random().Next(Summaries.Length)]}；服务地址：{Request.Host}";
            // 负载均衡时，打印出来方便看到调用了哪个服务
            Console.WriteLine(summary);
            return summary;
        }


        [Polly]
        [HttpGet]
        [Route("/GetAllServices")]
        public async Task<List<string>> GetAllServicesAsync()
        {
            var consuleClient = new ConsulClient(consulConfig =>
            {
                consulConfig.Address = new Uri("http://127.0.0.1:8500");
            });
            var queryResult = await consuleClient.Health.Service("MicrServiceDemo", "", true);
            var result = new List<string>();
            foreach (var service in queryResult.Response)
            {
                result.Add(service.Service.Address + ":" + service.Service.Port);
            }
            return result;
        }
    }
}
