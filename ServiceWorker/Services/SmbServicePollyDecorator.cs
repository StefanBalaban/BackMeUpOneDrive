using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Exceptions;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using Polly;
using Polly.Retry;
using SMBLibrary;

namespace BackMeUp.ServiceWorker.Services
{
    public class SmbServicePollyDecorator : ISmbService
    {
        private readonly ISmbService _inner;
        private readonly ILogger<SmbServicePollyDecorator> _logger;
        private readonly RetryPolicy _retry;

        public SmbServicePollyDecorator(ISmbService inner, NetworkConfiguration networkConfiguration,
            ILogger<SmbServicePollyDecorator> logger)
        {
            _inner = inner;

            _retry = Policy
                .Handle<SmbFileException>()
                .Or<SmbException>()
                .Or<Exception>()
                .Retry(networkConfiguration.Retries);

            _logger = logger;
        }

        public void DeleteFile(FileDirectoryInformation file)
        {
            try
            {
                _retry.Execute(() => _inner.DeleteFile(file));
            }
            catch (SmbFileException ex)
            {
                _logger.LogError(
                    "Failed to access file that is set to be deleted. " +
                    "File status: {file}. " +
                    "NT status: {status}. " +
                    "Path: {path}",
                    ex.FileStatus,
                    ex.Status,
                    ex.Path);
                throw;
            }
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public void MoveFileToShare(DirectoryDownload directory)
        {
            try
            {
                _retry.Execute(() => _inner.MoveFileToShare(directory));
            }
            catch (SmbFileException ex)
            {
                _logger.LogError(
                    "Failed to create file on share. " +
                    "File status: {file}. " +
                    "NT status: {status}. " +
                    "Path: {path}",
                    ex.FileStatus,
                    ex.Status,
                    ex.Path);

                throw;
            }
            catch (SmbException ex)
            {
                _logger.LogError("Failed to write on created file. NT status: {status}", ex.Status);
                throw;
            }
        }


        public List<FileDirectoryInformation> GetListOfOldBackups(List<DirectoryDownload> directories,
            int numberOfBackups)
        {
            try
            {
                return _retry.Execute(() => _inner.GetListOfOldBackups(directories, numberOfBackups));
            }
            catch (SmbFileException ex)
            {
                _logger.LogError(
                    "Failed to access backups directory. " +
                    "File status: {file}. " +
                    "NT status: {status}. " +
                    "Path: {path}",
                    ex.FileStatus,
                    ex.Status,
                    ex.Path);
                throw;
            }
        }
    }
}