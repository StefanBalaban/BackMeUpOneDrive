using BackMeUp.ServiceWorker.Models;

namespace BackMeUp.ServiceWorker.Interfaces
{
    public interface IFileDownloadService
    {
        /// <summary>
        ///     Name of the cloud service that is being backed-up.
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        ///     Will be true once all of the pages al fetched from the underlying storage service.
        /// </summary>
        public bool AllFilesDownloaded { get; set; }

        /// <summary>
        ///     Downloads a page of files from an underlying storage service.
        /// </summary>
        public Task<List<FileDownload>> DownloadFilesAsync(CancellationToken stoppingToken);
    }
}