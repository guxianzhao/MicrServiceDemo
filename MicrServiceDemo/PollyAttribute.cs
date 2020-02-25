using Microsoft.AspNetCore.Mvc.Filters;
using Polly;
using System;

namespace MicrServiceDemo
{
    public class PollyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                Policy.Handle<Exception>().Retry(2, (ex, count) =>
                {
                    Console.WriteLine($"重试次数:{count}, 异常信息:{ex.Message}");
                }).Execute(() =>
                {
                    base.OnActionExecuting(context);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("抛出异常：" + ex.Message);
            }
        }
    }
}
