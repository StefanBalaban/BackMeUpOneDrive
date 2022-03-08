using BackMeUp.ServiceWorker.Models;

namespace BackMeUp.ServiceWorker.Interfaces
{
    public interface IGraphService
    {
        /// <summary>
        ///     Returns the list of all files ketp on a Microsoft cloud service, with their path, name, and size.
        /// </summary>
        Task<List<FileDownload>> GetFilesListAsync(CancellationToken stoppingToken);

        /// <summary>
        ///     Returns a singular file from a Microsoft cloud service.
        /// </summary>
        Task<Stream> DownloadFileAsync(string id, CancellationToken stoppingToken);
    }
}