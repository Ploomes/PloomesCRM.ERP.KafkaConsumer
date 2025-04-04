using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PloomesCRMCallbackHub2.Queue.DLL.BasicCommon;
using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using PloomesCRMCallbackHub2.Queue.DLL.Business;
using System.IO;
using Confluent.Kafka;
using PloomesCRMCallbackHub2.Queue.DLL.Repositories;

namespace PloomesCRMCallbackHub2.Queue.DLL
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            services.Configure<PloomesSettings>(Configuration.GetSection(PloomesSettings.SectionName));//Load Settings
            services.AddHttpClient(); //HttpClientFactory
            services.AddOptions();
            services.AddSingleton<KafkaConsumer>();//Create the Mail Background Service
            services.AddHostedService(provider => provider.GetService<KafkaConsumer>());//Run Mail Background Service
            services.AddSingleton<IDataAccess, DataAccess>();//Database access
            services.AddSingleton<IRedis, Redis>();//Database access
            
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
         
            app.UseHttpsRedirection();
            
            app.UseRouting();

            app.UseAuthorization();
            
        }

    }
}
