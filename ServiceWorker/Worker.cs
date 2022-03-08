using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using NCrontab;

namespace BackMeUp.ServiceWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceLocator _serviceLocator;
    private DateTime _nextRun;
    private readonly bool _runOnce;

    private readonly CrontabSchedule _schedule;

    public Worker(ILogger<Worker> logger, IServiceLocator serviceLocator, CronConfiguration cronConfiguration)
    {
        _schedule = CrontabSchedule.Parse(cronConfiguration.Schedule,
            new CrontabSchedule.ParseOptions {IncludingSeconds = true});
        _runOnce = cronConfiguration.RunOnce;
        _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
        _logger = logger;
        _serviceLocator = serviceLocator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var now = DateTime.Now;
            if (now > _nextRun)
            {
                try
                {
                    _logger.LogInformation("Backup process started");
                    await ProcessAsync(stoppingToken);
                    _logger.LogInformation("Backup process successfully completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "The service encountered an error, backup execution has stopped.");
                }

                _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                if (_runOnce)
                {
                    _logger.LogInformation("Backup process finished one-time run.");
#if TEST
                    // Unit tests break if StopAsync is called
                    break;
#endif
                    await StopAsync(stoppingToken);
                }
            }

            // 5 seconds delay
            await Task.Delay(5000, stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);

        await StopAsync(stoppingToken);
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        long completeDownloadSizeBytes = 0;

        var services = _serviceLocator.GetServices();

        var fileDownloadServices = services.Item2;
        var fileStorageService = services.Item1;

        var directories = new List<DirectoryDownload>();

        foreach (var service in fileDownloadServices)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Backup process for {service} has started.", service.ServiceName);

            long serviceDownloadSizeBytes = 0;

            DirectoryDownload? directory = new DirectoryDownload();
            directories.Add(directory);

            directory.ServiceName = service.ServiceName;

            try
            {
                // Temp directory used for the custom paging, files placed here to be zipped later on
                directory.TempPath = Path.Combine(Path.GetTempPath(), directory.GetServiceNameWithDate());
                fileStorageService.CreateDirectory(directory.TempPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Backup process encountered error during the creation of the temp directory for {service}",
                    service.ServiceName);
                throw;
            }

            while (!service.AllFilesDownloaded)
            {
                var files = await service.DownloadFilesAsync(stoppingToken);

                if (files.Count == 0)
                {
                    break;
                }

                serviceDownloadSizeBytes += files.Sum(x => x.SizeBytes);

                for (int i = 0; i < files.Count; i++)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    try
                    {
                        await fileStorageService.WriteFileToDirectoryAsync(files[i], directory.TempPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            "Backup process encountered error during the creation of the temp directory for {service}",
                            service.ServiceName);
                        throw;
                    }
                }


                // Remove refference from the current list of downloaded files to promote it to Garbage
                files = new List<FileDownload>();
                // Force GC to make sure the memory is cleared of garbage.
                // The performance penalty hit is acceptable to prevent memory being cluttered with Garbage
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            stoppingToken.ThrowIfCancellationRequested();

            completeDownloadSizeBytes += serviceDownloadSizeBytes;

            _logger.LogInformation(
                "Backup size for {service} is {bytes} bytes ({megabytes} megabytes)",
                service.ServiceName,
                serviceDownloadSizeBytes,
                serviceDownloadSizeBytes / 1024 / 1024);
        }

        _logger.LogInformation(
            "Download process for {number} service(s) has completed. " +
            "Total backup size is {bytes} bytes ({megabytes} megabytes).",
            fileDownloadServices.Count(),
            completeDownloadSizeBytes,
            completeDownloadSizeBytes / 1024 / 1024);


        fileStorageService.ZipFiles(directories, stoppingToken);
        fileStorageService.MoveZipFiles(directories, stoppingToken);
        fileStorageService.DeleteTempFiles(directories, stoppingToken);
        fileStorageService.DeleteOldBackups(directories, stoppingToken);
    }
}