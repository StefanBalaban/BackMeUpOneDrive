using BackMeUp.ServiceWorker;
using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Services;
using Serilog;
using Serilog.Core;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Pulls configurations from appsettings.json/environment variables 
        IConfiguration configuration = hostContext.Configuration;
        var cronConfiguration = configuration.GetSection("Cron").Get<CronConfiguration>();
        var sessionConfiguration = configuration.GetSection("Session").Get<SessionConfiguration>();
        var fileStorageConfiguration = configuration.GetSection("FileStorage").Get<FileStorageConfiguration>();
        var oneDriveConfiguration = configuration.GetSection("OneDrive").Get<OneDriveConfiguration>();
        var networkConfiguration = configuration.GetSection("Network").Get<NetworkConfiguration>();
        var smbConfiguration = configuration.GetSection("Smb").Get<SmbConfiguration>();

        services.AddSingleton(cronConfiguration);
        services.AddSingleton(sessionConfiguration);
        services.AddSingleton(fileStorageConfiguration);
        services.AddSingleton(oneDriveConfiguration);
        services.AddSingleton(networkConfiguration);
        services.AddSingleton(smbConfiguration);
        services.AddSingleton<IServiceLocator, ServiceLocator>();

        services.AddHostedService<Worker>();

        services.AddHttpClient<GraphService>().SetHandlerLifetime(Timeout.InfiniteTimeSpan)
            .ConfigureHttpClient(x => x.Timeout = TimeSpan.FromMinutes(3));
        services.AddHttpClient<SessionService>();
        services.AddScoped<IFileDownloadService, OneDriveService>();
        services.AddScoped<ISessionService, SessionService>();
        services.Decorate<ISessionService, SessionServicePollyDecorator>();
        services.AddScoped<IGraphService, GraphService>();
        services.Decorate<IGraphService, GraphServicePollyDecorator>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ISmbService, SmbService>();
        services.Decorate<ISmbService, SmbServicePollyDecorator>();
    })
    .ConfigureLogging((hostContext, logging) =>
    {
        var configuration = hostContext.Configuration;

        if (Convert.ToBoolean(hostContext.Configuration["Serilog:UseSerilog"]))
        {
            logging.ClearProviders();
            Logger? logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            logging.AddSerilog(logger, dispose: true);
        }
    })
    .Build();

await host.RunAsync();