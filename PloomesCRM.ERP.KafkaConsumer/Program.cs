using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PloomesCRMCallbackHub2.Queue.DLL.BasicCommon;
using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using PloomesCRMCallbackHub2.Queue.DLL.Business;
using PloomesCRMCallbackHub2.Queue.DLL.Repositories;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Move your ConfigureServices logic here
        services.Configure<PloomesSettings>(hostContext.Configuration.GetSection(PloomesSettings.SectionName));
        services.AddHttpClient();
        services.AddOptions();
        services.AddSingleton<KafkaConsumer>();
        services.AddSingleton<IDataAccess, DataAccess>();
        services.AddSingleton<IRedis, Redis>();
        services.AddHostedService<KafkaConsumer>();
        
    }).Build();

host.Run();