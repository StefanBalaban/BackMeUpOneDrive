using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using Microsoft.Graph;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BackMeUp.ServiceWorker.Services;

public class GraphService : IGraphService
{
    private GraphServiceClient? _graphServiceClient;
    private readonly HttpClient _httpClient;
    private ILogger<GraphService> _logger;
    private readonly ISessionService _sessionService;

    public GraphService(ISessionService sessionService, HttpClient httpClient, ILogger<GraphService> logger)
    {
        _sessionService = sessionService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<Models.FileDownload>> GetFilesListAsync(CancellationToken stoppingToken)
    {
        await CreateGraphServiceClientAsync(stoppingToken);

        string? folderPageId = (await _graphServiceClient.Me.Drive.Root
            .Request()
            .GetAsync()).Id;


        List<FileDownload>? files = new List<FileDownload>();

        files.AddRange(await GetFilesFromFolderPageRecusivelly(folderPageId, "", stoppingToken));
        return files;
    }

    public async Task<Stream> DownloadFileAsync(string fileId, CancellationToken stoppingToken)
    {
        await CreateGraphServiceClientAsync(stoppingToken);

        if (fileId == null)
        {
            throw new ArgumentNullException(nameof(fileId));
        }

        try
        {
            return await _graphServiceClient
                .Me.Drive.Items[fileId].Content
                .Request()
                .GetAsync();
        }
        // If the token expires during download the catch block should attempt to reauthenticate
        catch (ServiceException ex)
        {
            _logger.LogWarning(
                ex,
                "Download of file: {fileId} failed due to an Graph API error. " +
                "HTTP status code: {code}. " +
                "Raw response body: {raw}",
                fileId,
                ex.StatusCode,
                ex.RawResponseBody);

            _logger.LogInformation("Attempting to reuathenticate Graph API cleint.");

            await CreateGraphServiceClientAsync(stoppingToken, true);

            return await _graphServiceClient
                .Me.Drive.Items[fileId].Content
                .Request()
                .GetAsync();
        }
    }

    /// <summary>
    ///     Asynchronous DFS that takes a folders Id, gets Id's of all files within it, and traverses trough all subfolders
    ///     recursively
    /// </summary>
    private async Task<List<Models.FileDownload>> GetFilesFromFolderPageRecusivelly(string folderId, string path,
        CancellationToken stoppingToken)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }


        if (string.IsNullOrEmpty(folderId) || stoppingToken.IsCancellationRequested)
        {
            return new List<FileDownload>();
        }


        List<FileDownload>? files = new List<FileDownload>();

        IDriveItemChildrenCollectionPage folderPage = null;
        string folderName = string.Empty;

        try
        {
            folderPage = await _graphServiceClient.Me.Drive.Items[folderId].Children.Request().GetAsync();
            folderName = (await _graphServiceClient.Me.Drive.Items[folderId].Request().GetAsync()).Name;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(
                ex,
                "Download of files from folder {id}, due to an Graph API error. " +
                "HTTP status code: {code}. " +
                "Raw response body: {raw}",
                folderId,
                ex.StatusCode,
                ex.RawResponseBody);
            throw;
        }

        string? currentPathWithFolderName = $"{path}{folderName}/";
        bool morePages;

        do
        {
            morePages = false;

            files.AddRange(
                folderPage
                    .Where(x => x.Folder == null)
                    .Select(x => new Models.FileDownload
                    {
                        Name = x.Name,
                        Id = x.Id,
                        SizeBytes = x.Size.HasValue ? x.Size.Value : 0,
                        Path = currentPathWithFolderName
                    })
            );

            var subFoldersId = folderPage.Where(x => x.Folder != null).Select(x => x.Id).ToList();
            for (int i = 0; i < subFoldersId.Count; i++)
            {
                files.AddRange(await GetFilesFromFolderPageRecusivelly(subFoldersId[i], currentPathWithFolderName,
                    stoppingToken));
            }

            if (folderPage.NextPageRequest != null)
            {
                folderPage = await folderPage.NextPageRequest.GetAsync();
                morePages = true;
            }
        } while (morePages);

        return files;
    }

    /// <summary>
    ///     Creates a new instance of the Graph API client with approapriate authentication.
    /// </summary>
    /// <param name="recreate">Used to force recreation of the Graph API client.</param>
    private async Task CreateGraphServiceClientAsync(CancellationToken stoppingTokenbool, bool recreate = false)
    {
        try
        {
            if (_graphServiceClient != null && !recreate && _sessionService.IsValidAccessToken())
            {
                return;
            }

            ;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                await _sessionService.GetAccessTokenAsync(stoppingTokenbool));

            // This fixes a GraphServiceClient that is caused by the fact thhat this custom auth implementation has a null auth provider.
            _httpClient.DefaultRequestHeaders.Add(CoreConstants.Headers.FeatureFlag,
                Enum.Format(typeof(FeatureFlag), FeatureFlag.AuthHandler, "x"));

            _graphServiceClient = new GraphServiceClient(_httpClient);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during Graph API client authentication.");
            throw;
        }
    }
}