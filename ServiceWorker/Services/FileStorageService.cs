using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using SMBLibrary;
using System.Collections.Generic;
using System.IO.Compression;

namespace BackMeUp.ServiceWorker.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageConfiguration _fileStorageConfiguration;
        private ILogger<FileStorageService> _logger;
        private readonly ISmbService _smbService;

        public FileStorageService(FileStorageConfiguration fileStorageConfiguration, ISmbService smbService,
            ILogger<FileStorageService> logger)
        {
            _fileStorageConfiguration = fileStorageConfiguration;
            _smbService = smbService;
            _logger = logger;
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void Dispose()
        {
            _smbService?.Dispose();
        }

        public void DeleteOldBackups(List<DirectoryDownload> directories, CancellationToken stoppingToken)
        {
            if (directories == null)
            {
                throw new ArgumentNullException(nameof(directories));
            }

            List<FileDirectoryInformation>? filesToDelete =
                _smbService.GetListOfOldBackups(directories, _fileStorageConfiguration.NumberOfBackups);


            foreach (var file in filesToDelete)
            {
                stoppingToken.ThrowIfCancellationRequested();
                _smbService.DeleteFile(file);
            }
        }

        public void DeleteTempFiles(List<DirectoryDownload> directories, CancellationToken stoppingToken)
        {
            if (directories == null)
            {
                throw new ArgumentNullException(nameof(directories));
            }

            foreach (var directory in directories)
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(directory.TempPath) && Directory.Exists(directory.TempPath))
                {
                    Directory.Delete(directory.TempPath, true);
                    _logger.LogInformation("Temp directory {path} has been successfully deleted.", directory.TempPath);
                }
                else
                {
                    _logger.LogWarning("Temp directory {path} not found.", directory.TempPath);
                }

                if (string.IsNullOrEmpty(directory.ZippedPath) && File.Exists(directory.ZippedPath))
                {
                    File.Delete(directory.ZippedPath);
                    _logger.LogInformation("Temp zip file {path} has been successfully deleted.", directory.ZippedPath);
                }
                else
                {
                    _logger.LogWarning("Temp zip file {path} not found.", directory.TempPath);
                }
            }
        }

        public void MoveZipFiles(List<DirectoryDownload> directories, CancellationToken stoppingToken)
        {
            if (directories == null)
            {
                throw new ArgumentNullException(nameof(directories));
            }

            foreach (var directory in directories)
            {
                stoppingToken.ThrowIfCancellationRequested();

                _smbService.MoveFileToShare(directory);
            }
        }

        public void ZipFiles(List<DirectoryDownload> directories, CancellationToken stoppingToken)
        {
            if (directories == null)
            {
                throw new ArgumentNullException(nameof(directories));
            }


            foreach (var directory in directories)
            {
                stoppingToken.ThrowIfCancellationRequested();

                directory.ZippedPath = directory.TempPath + ".zip";
                ZipFile.CreateFromDirectory(directory.TempPath, directory.ZippedPath, CompressionLevel.SmallestSize,
                    false);
            }
        }

        public async Task WriteFileToDirectoryAsync(FileDownload fileDownload, string path)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(path, fileDownload.Path));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Backup process encountered an error during the creation of temp directories for path: {path}",
                    Path.Combine(path, fileDownload.Path));
                throw;
            }

            try
            {
                string filePath = Path.Combine(path, fileDownload.Path, fileDownload.Name);
                using FileStream outputFileStream = new FileStream(filePath, FileMode.Create);
                await fileDownload.Bytes.CopyToAsync(outputFileStream);
                outputFileStream.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Backup process encountered an error during the writting of file {file} in path: {path}",
                    fileDownload.Name, Path.Combine(path, fileDownload.Path));
                throw;
            }
        }
    }
}