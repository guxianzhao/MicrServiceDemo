using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ProductServer.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        [CapSubscribe("Commit.Order")]
        public void ReceivedOrderCommit(DtoOrder order)
        {
            Console.WriteLine(JsonConvert.SerializeObject(order));
        }
    }

    public class DtoOrder
    {
        public int ProductID { get; set; }
        public int UserID { get; set; }
        public int Amount { get; set; }
    }
}