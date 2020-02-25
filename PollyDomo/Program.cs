using Polly;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PollyDomo
{
    class Program
    {
        static void Main(string[] args)
        {
            TestPolly();
            Console.ReadLine();
        }

        public static void Case4()
        {
            try
            {
                var policyException = Policy.Handle<Exception>()
                    .Fallback(() =>
                    {
                        Console.WriteLine("Fallback");
                    });

                var policyTimeout = Policy.Timeout(3, TimeoutStrategy.Pessimistic);
                Policy.Wrap(policyTimeout, policyException).Execute(() =>
                {
                    Console.WriteLine("Job Start...");
                    Thread.Sleep(5000);
                    //throw new Exception();
                    Console.WriteLine("Job End...");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception : {ex.GetType()} : {ex.Message}");
            }
        }

        static void GetServiceResult()
        {
            var content = new HttpClient().GetStringAsync("http://localhost:6002/weather").Result;
            Console.WriteLine($"调用结果：{content}");
        }

        static void TestPolly()
        {
            var policy = CreatePolly();

            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine($"-------------第{i}次请求-------------");
                policy.Execute(() =>
                {
                    // 从10次开始，正常请求成功
                    if (i < 10)
                    {
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}：请求成功");
                    }
                });
                Thread.Sleep(1000);
            }
        }

        public static ISyncPolicy CreatePolly()
        {
            // 超时1秒
            var timeoutPolicy = Policy.Timeout(1, TimeoutStrategy.Pessimistic, (context, timespan, task) =>
            {
                Console.WriteLine("执行超时，抛出TimeoutRejectedException异常");
            });


            // 重试2次
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetry(
                    2,
                    retryAttempt => TimeSpan.FromSeconds(2),
                    (exception, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"{DateTime.Now} - 重试 {retryCount} 次 - 抛出{exception.GetType()}-{timespan.TotalMilliseconds}");
                    });

            // 连续发生两次故障，就熔断3秒
            var circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreaker(
                    // 熔断前允许出现几次错误
                    2,
                    // 熔断时间
                    TimeSpan.FromSeconds(5),
                    // 熔断时触发 OPEN
                    onBreak: (ex, breakDelay) =>
                    {
                        Console.WriteLine($"{DateTime.Now} - 断路器：开启状态（熔断时触发）");
                    },
                    // 熔断恢复时触发 // CLOSE
                    onReset: () =>
                    {
                        Console.WriteLine($"{DateTime.Now} - 断路器：关闭状态（熔断恢复时触发）");
                    },
                    // 熔断时间到了之后触发，尝试放行少量（1次）的请求，
                    onHalfOpen: () =>
                    {
                        Console.WriteLine($"{DateTime.Now} - 断路器：半开启状态（熔断时间到了之后触发）");
                    }
                );

            // 回退策略，降级！
            var fallbackPolicy = Policy.Handle<Exception>()
                .Fallback(() =>
                {
                    Console.WriteLine("这是一个Fallback");
                }, exception =>
                {
                    Console.WriteLine($"Fallback异常：{exception.GetType()}");
                });

            // 策略从右到左依次进行调用
            // 首先判断调用是否超时，如果超时就会触发异常，发生超时故障，然后就触发重试策略；
            // 如果重试两次中只要成功一次，就直接返回调用结果
            // 如果重试两次都失败，第三次再次失败，就会发生故障
            // 重试之后是断路器策略，所以这个故障会被断路器接收，当断路器收到两次故障，就会触发熔断，也就是说断路器开启
            // 断路器开启的3秒内，任何故障或者操作，都会通过断路器到达回退策略，触发降级操作
            // 3秒后，断路器进入到半开启状态，操作可以正常执行
            return Policy.Wrap(fallbackPolicy, circuitBreakerPolicy, retryPolicy, timeoutPolicy);
        }

        /// <summary>
        /// Polly方法使用代码Demo
        /// </summary>
        /// <returns></returns>
        static async Task PollyCodeDemoAsync()
        {
            Policy.Handle<Exception>().WaitAndRetry(
              3,
              retryAttempt => TimeSpan.FromSeconds(5),
              // 处理异常、
              (exception, timespan, retryCount, context) =>
              {
                  // doSomething
                  Console.WriteLine($"{DateTime.Now} - 重试 {retryCount} 次 - 抛出{exception.GetType()}-{timespan.TotalMilliseconds}");
              })
            .Execute(() =>
            {
                GetServiceResult();
            });
            Policy.Handle<Exception>()
                 .CircuitBreaker(
                     // 熔断前允许出现几次错误
                     3,
                     // 熔断时间
                     TimeSpan.FromSeconds(5),
                     // 熔断时触发
                     onBreak: (ex, breakDelay) =>
                     {
                         Console.WriteLine("断路器：开启状态（熔断时触发）");
                     },
                     // 熔断恢复时触发
                     onReset: () =>
                     {
                         Console.WriteLine("断路器：关闭状态（熔断恢复时触发）");
                     },
                     // 熔断时间到了之后触发，尝试放行少量（1次）的请求，
                     onHalfOpen: () =>
                     {
                         Console.WriteLine("断路器：半开启状态（熔断时间到了之后触发）");
                     }
                 );


            Policy.Handle<Exception>().CircuitBreaker(5, TimeSpan.FromSeconds(10))
            .Execute(() =>
            {
                // do something
            });

            // 单个异常类型
            Policy.Handle<Exception>();

            // 限定条件的单个异常
            Policy.Handle<ArgumentException>(ex => ex.ParamName == "ID");

            // 多个异常类型
            Policy.Handle<Exception>().Or<ArgumentException>();

            // 限定条件的多个异常
            Policy.Handle<Exception>(ex => ex.Message == "请求超时")
                .Or<ArgumentException>(ex => ex.ParamName == "ID");

            // Inner Exception 异常里面的异常类型 
            Policy.HandleInner<Exception>()
                .OrInner<ArgumentException>(ex => ex.ParamName == "ID");

            // 返回结果加限定条件 
            Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound);

            // 处理多个返回结果
            Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.InternalServerError)
                .OrResult(r => r.StatusCode == HttpStatusCode.BadGateway);

            // 处理元类型结果 (用.Equals)
            Policy.HandleResult(HttpStatusCode.InternalServerError)
                .OrResult(HttpStatusCode.BadGateway);

            // 重试1次
            Policy.Handle<Exception>().Retry();

            // 重试3次
            Policy.Handle<Exception>().Retry(3);

            // 重试3次，加上重试时的action参数
            Policy.Handle<Exception>().Retry(3, (exception, retryCount) =>
            {
                // do Something
            });

            // 不断重试,直到成功
            Policy.Handle<Exception>().RetryForever();

            // 不断重试，带action参数在每次重试的时候执行
            Policy.Handle<Exception>().RetryForever(exception =>
            {
                // do Something       
            });

            // 重试3次，每次等待5s
            Policy.Handle<Exception>().WaitAndRetry(
                3,
                retryAttempt => TimeSpan.FromSeconds(5),
                // 处理异常、等待时间、重试第几次
                (exception, timespan, retryCount, context) =>
                {
                    // do Something
                });

            // 重试3次，分别等待1、2、3秒。
            Policy.Handle<Exception>().WaitAndRetry(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3)
            });

            Policy.Handle<Exception>().CircuitBreaker(2, TimeSpan.FromSeconds(10));
            Policy.Handle<Exception>().Fallback(() =>
            {
                // do something
            });

            // 设置超时时间为30s
            Policy.Timeout(30, onTimeout: (context, timespan, task, ex) =>
            {
                // do something 
            });

            // 超时分为乐观超时与悲观超时，乐观超时依赖于CancellationToken ，它假设我们的具体执行的任务都支持CancellationToken。
            // 那么在进行timeout的时候，它会通知执行线程取消并终止执行线程，避免额外的开销。下面的乐观超时的具体用法 。
            HttpResponseMessage httpResponse = await Policy.TimeoutAsync(30)
            .ExecuteAsync(
                async ct => await new HttpClient().GetAsync(""),
                CancellationToken.None
            );

            // 悲观超时与乐观超时的区别在于，如果执行的代码不支持取消CancellationToken，
            // 它还会继续执行，这会是一个比较大的开销。
            Policy.Timeout(30, TimeoutStrategy.Pessimistic);



            Policy.Bulkhead(12);
            // 同时，我们还可以控制一个等待处理的队列长度
            Policy.Bulkhead(12, 2);

            // 以及当请求执行操作被拒绝的时候，执行回调
            Policy.Bulkhead(12, context =>
            {
                // do something 
            });

            //var policyWrap = Policy.Wrap(fallback, breaker, timeout, retry, bulkhead);
            //policyWrap.Execute(() =>
            //{
            //    // do something
            //});
        }

    }
}
