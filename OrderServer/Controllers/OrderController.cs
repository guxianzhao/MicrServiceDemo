using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OrderServer.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _dbContext;
        private readonly ICapPublisher _capBus;
        public OrderController(OrderDbContext dbContext, ICapPublisher capBus)
        {
            _dbContext = dbContext;
            _capBus = capBus;
        }

        public IActionResult Test()
        {
            return Ok();
        }

        [HttpPost]
        public IActionResult Save([FromBody]DtoOrder dto)
        {
            using (var trans = _dbContext.Database.BeginTransaction(_capBus, autoCommit: false))
            {
                //业务代码
                var model = new Orders()
                {
                    Amount = dto.Amount,
                    ProductID = dto.ProductID,
                    UserID = dto.UserID
                };
                _dbContext.Orders.Add(model);

                _capBus.Publish("Commit.Order", model);
                trans.Commit();
            }
            return Ok();
        }

        public class DtoOrder
        {
            public int ProductID { get; set; }
            public int UserID { get; set; }
            public int Amount { get; set; }
        }
    }
}