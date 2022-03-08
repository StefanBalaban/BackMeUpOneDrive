using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using BackMeUp.ServiceWorker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SMBLibrary;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BackMeUp.UnitTests
{
    public class FileStorageServiceUnitTests : IDisposable, IAsyncLifetime
    {
        private List<FileDirectoryInformation> _fileDorectoryInformationList;
        private FileStorageConfiguration _fileStorageConfiguration;
        private FileStorageService _fileStorageService;
        private Mock<ILogger<FileStorageService>> _logger;
        private Mock<ISmbService> _smbService;

        public async Task DisposeAsync()
        {
            Dispose();
        }

        public async Task InitializeAsync()
        {
            _fileStorageConfiguration = new FileStorageConfiguration {NumberOfBackups = 3};
            _smbService = new Mock<ISmbService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<FileStorageService>>();

            _fileDorectoryInformationList = new List<FileDirectoryInformation> {new(), new()};

            _smbService.Setup(x => x.Dispose());
            _smbService.Setup(x => x.MoveFileToShare(It.IsNotNull<DirectoryDownload>()));
            _smbService.Setup(x => x.GetListOfOldBackups(It.IsNotNull<List<DirectoryDownload>>(), It.IsAny<int>()))
                .Returns(_fileDorectoryInformationList);
            _smbService.Setup(x => x.DeleteFile(It.IsNotNull<FileDirectoryInformation>()));

            _fileStorageService = new FileStorageService(_fileStorageConfiguration, _smbService.Object, _logger.Object);
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task DeleteOldBackups_CalledWithProperParameters_SmbServiceDeleteFileInvoked()
        {
            List<DirectoryDownload>? directories = new List<DirectoryDownload> {new(), new()};

            _fileStorageService.DeleteOldBackups(directories, new CancellationToken());

            _smbService.Verify(x => x.DeleteFile(It.IsNotNull<FileDirectoryInformation>()),
                Times.Exactly(_fileDorectoryInformationList.Count));
        }

        [Fact]
        public async Task DeleteOldBackups_CalledWithProperParameters_SmbServiceGetListOfOldbackupsInvoked()
        {
            List<DirectoryDownload>? directories = new List<DirectoryDownload> {new(), new()};

            _fileStorageService.DeleteOldBackups(directories, new CancellationToken());

            _smbService.Verify(x =>
                x.GetListOfOldBackups(
                    It.Is<List<DirectoryDownload>>(x => x.Count == directories.Count),
                    It.Is<int>(x => x == _fileStorageConfiguration.NumberOfBackups)));
        }

        [Fact]
        public async Task DeleteOldBackups_CalledWithImproperParameters_ArgumentNullExceptionThrown()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _fileStorageService.DeleteOldBackups(null, new CancellationToken()));
        }

        [Fact]
        public async Task MoveZipFiles_CalledWithTwoDirectories_SmbServiceMoveFileToShareInvokedTwoTimes()
        {
            List<DirectoryDownload>? directories = new List<DirectoryDownload> {new(), new()};

            _fileStorageService.MoveZipFiles(directories, new CancellationToken());

            _smbService.Verify(x => x.MoveFileToShare(It.IsAny<DirectoryDownload>()), Times.Exactly(directories.Count));
        }
    }
}