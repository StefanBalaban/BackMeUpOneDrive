using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;

namespace BackMeUp.ServiceWorker.Services;

public class OneDriveService : IFileDownloadService
{
    private readonly long _pageSize;
    private List<Models.FileDownload>? _files;
    private int _filesIndex;
    private readonly IGraphService _graphService;
    private ILogger<OneDriveService> _logger;
    private int _page;

    public OneDriveService(IGraphService graphService, OneDriveConfiguration oneDriveConfiguration,
        ILogger<OneDriveService> logger)
    {
        _graphService = graphService;
        _pageSize = 1024 * 1024 * oneDriveConfiguration.PageSizeInMegabytes;
        ServiceName = "OneDrive";
        _page = 1;
        _logger = logger;
    }


    public bool AllFilesDownloaded { get; set; }

    public string ServiceName { get; set; }

    /// <summary>
    ///     Downloads a page of files from OneDrive
    /// </summary>
    public async Task<List<Models.FileDownload>> DownloadFilesAsync(CancellationToken stoppingToken)
    {
        if (_files == null)
        {
            _files = await _graphService.GetFilesListAsync(stoppingToken);

            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "List of files for download, from OneDrive, is ready. Number of files to download: {number}",
                _files.Count);
        }

        long currentSize = 0;

        var downloadedFiles = new List<Models.FileDownload>();


        // While the current size of objects in memory is less than the page size, add files to download list
        while (_filesIndex < _files.Count && currentSize + _files[_filesIndex].SizeBytes <= _pageSize &&
               !stoppingToken.IsCancellationRequested)
        {
            downloadedFiles.Add(new Models.FileDownload
            {
                Bytes = await _graphService.DownloadFileAsync(_files[_filesIndex].Id, stoppingToken),
                Id = _files[_filesIndex].Id,
                Name = _files[_filesIndex].Name,
                SizeBytes = _files[_filesIndex].SizeBytes,
                Path = _files[_filesIndex].Path
            });

            currentSize += _files[_filesIndex].SizeBytes;
            _filesIndex++;
        }

        stoppingToken.ThrowIfCancellationRequested();

        // If OneDrive contains a file that is larger than the paging restriction
        if (_filesIndex < _files.Count && downloadedFiles.Count == 0)
        {
            throw new Exception("File too big for page setting");
        }

        if (_filesIndex == _files.Count)
        {
            AllFilesDownloaded = true;
        }


        _logger.LogInformation(
            "Download of {files} file(s) from one drive complete for page {page}. " +
            "Number of files left for download: {number}",
            downloadedFiles.Count,
            _page++,
            _files.Count - _filesIndex
        );


#if DEBUG
        // For debuging purposes, when coupled with small MB page size it will quickly download 2 pages only
        if (_page == 3)
        {
            AllFilesDownloaded = true;
        }
#endif


        return downloadedFiles;
    }
}