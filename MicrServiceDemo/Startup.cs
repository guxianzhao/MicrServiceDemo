using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicrServiceDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            // ��ӽ������
            services.AddHealthChecks();
            services.Configure<ConsulServiceOptions>(new ConfigurationBuilder()
                .AddJsonFile("consulconfig.json").Build());

            services.AddMvc(new Action<MvcOptions>(r =>
            {
                r.Filters.Add(typeof(PollyAttribute));
            }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ��ȡconsul��������Ϣ
            var serviceOptions = app.ApplicationServices.GetRequiredService<IOptions<ConsulServiceOptions>>();
            // ���ý�������ַ��.NET Core ���õĽ�������ַ�м��
            //app.UseHealthChecks(serviceOptions.Value.HealthCheck);
            //app.UseConsul(Configuration);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
