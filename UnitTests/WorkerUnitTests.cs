using BackMeUp.ServiceWorker;
using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using BackMeUp.UnitTests.Stubs;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BackMeUp.UnitTests
{
    public class WorkerUnitTests : IDisposable, IAsyncLifetime
    {
        private List<FileDownload> _fileDonwloadList;
        private FileDownload _fileDownload;
        private Mock<IFileDownloadService> _fileDownloadService;
        private Mock<IFileStorageService> _fileStorageService;
        private Mock<ILogger<Worker>> _logger;
        private string _serviceName;
        private Mock<IServiceLocator> _serviceProvider;
        private WorkerStub _worker;

        public async Task DisposeAsync()
        {
            Dispose();
        }

        public async Task InitializeAsync()
        {
            _logger = new Mock<ILogger<Worker>>(MockBehavior.Loose);
            _fileStorageService = new Mock<IFileStorageService>(MockBehavior.Strict);
            _fileDownloadService = new Mock<IFileDownloadService>(MockBehavior.Strict);
            _serviceProvider = new Mock<IServiceLocator>(MockBehavior.Strict);


            _fileStorageService.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            _fileStorageService
                .Setup(x => x.WriteFileToDirectoryAsync(It.IsNotNull<FileDownload>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            _fileStorageService.Setup(x =>
                x.ZipFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()));
            _fileStorageService.Setup(x =>
                x.MoveZipFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()));
            _fileStorageService.Setup(x =>
                x.ZipFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()));
            _fileStorageService.Setup(x =>
                x.DeleteOldBackups(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()));
            _fileStorageService.Setup(x =>
                x.DeleteTempFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()));
            _fileStorageService.Setup(x =>
                x.DeleteTempFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()));

            _serviceName = "TestService";
            _fileDownload =
                new FileDownload
                {
                    Bytes = Stream.Null,
                    Id = "123",
                    Name = "321",
                    Path = "Test",
                    SizeBytes = new Random(12).Next()
                };
            _fileDonwloadList = new List<FileDownload> {_fileDownload};

            // Two download pages
            _fileDownloadService.SetupSequence(x => x.AllFilesDownloaded).Returns(false).Returns(false).Returns(true);
            _fileDownloadService.Setup(x => x.ServiceName).Returns(_serviceName);
            _fileDownloadService.Setup(x => x.DownloadFilesAsync(It.IsNotNull<CancellationToken>()))
                .ReturnsAsync(_fileDonwloadList);

            _serviceProvider
                .Setup(x => x.GetServices())
                .Returns(
                    new Tuple<IFileStorageService, IEnumerable<IFileDownloadService>>(
                        _fileStorageService.Object,
                        new List<IFileDownloadService> {_fileDownloadService.Object}
                    ));


            _worker = new WorkerStub(_logger.Object, _serviceProvider.Object,
                new CronConfiguration {Schedule = "*/10 * * * * *", RunOnce = true});
        }

        public void Dispose()
        {
            _worker = null;
        }

        [Fact]
        public async Task ExecuteAsync_CalledWithTwoPages_AllDependenciesInvoked()
        {
            await _worker.ExecuteAsync(new CancellationToken());

            _serviceProvider.Verify(x => x.GetServices(), Times.Once());

            _fileDownloadService.Verify(x => x.ServiceName, Times.Exactly(3));

            _fileStorageService.Verify(x => x.CreateDirectory(It.IsNotNull<string>()), Times.Exactly(1));

            _fileDownloadService.Verify(x => x.AllFilesDownloaded, Times.Exactly(3));

            _fileDownloadService.Verify(x => x.DownloadFilesAsync(It.IsNotNull<CancellationToken>()), Times.Exactly(2));

            _fileStorageService.Verify(
                x => x.WriteFileToDirectoryAsync(It.IsNotNull<FileDownload>(), It.IsNotNull<string>()),
                Times.Exactly(2));

            _fileStorageService.Verify(
                x => x.ZipFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()),
                Times.Once);

            _fileStorageService.Verify(
                x => x.MoveZipFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()),
                Times.Once);

            _fileStorageService.Verify(
                x => x.DeleteTempFiles(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()),
                Times.Once);

            _fileStorageService.Verify(
                x => x.DeleteOldBackups(It.IsNotNull<List<DirectoryDownload>>(), It.IsNotNull<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Called_CreateDirectoryContainsServiceName()
        {
            await _worker.ExecuteAsync(new CancellationToken());

            _fileStorageService.Verify(x => x.CreateDirectory(It.Is<string>(x => x.Contains(_serviceName))));
        }

        [Fact]
        public async Task ExecuteAsync_Called_WriteFileToDirectoryAsyncCalledWithProperParameters()
        {
            await _worker.ExecuteAsync(new CancellationToken());

            _fileStorageService.Verify(x =>
                x.WriteFileToDirectoryAsync(It.Is<FileDownload>(y =>
                        y.Path.Equals(_fileDownload.Path) &&
                        y.Id.Equals(_fileDownload.Id) &&
                        y.Name.Equals(_fileDownload.Name) &&
                        y.SizeBytes.Equals(_fileDownload.SizeBytes)
                    ), It.Is<string>(y => y.Contains(_serviceName))
                ));
        }

        [Fact]
        public async Task ExecuteAsync_Called_MoveZipFilesCalledWithProperDirectoryDownloadParameters()
        {
            await _worker.ExecuteAsync(new CancellationToken());

            _fileStorageService.Verify(x =>
                x.MoveZipFiles(It.Is<List<DirectoryDownload>>(y =>
                        y.Count == 1 &&
                        y.FirstOrDefault().ServiceName.Equals(_serviceName) &&
                        y.FirstOrDefault().TempPath.Contains(_serviceName)
                    ), It.IsNotNull<CancellationToken>()
                ));
        }

        [Fact]
        public async Task ExecuteAsync_Called_DeleteTempetFilesCalledWithProperDirectoryDownloadParameters()
        {
            await _worker.ExecuteAsync(new CancellationToken());

            _fileStorageService.Verify(x =>
                x.DeleteTempFiles(It.Is<List<DirectoryDownload>>(y =>
                        y.Count == 1 &&
                        y.FirstOrDefault().ServiceName.Equals(_serviceName) &&
                        y.FirstOrDefault().TempPath.Contains(_serviceName)
                    ), It.IsNotNull<CancellationToken>()
                ));
        }

        [Fact]
        public async Task ExecuteAsync_Called_DeleteOldBackupsCalledWithProperDirectoryDownloadParameters()
        {
            await _worker.ExecuteAsync(new CancellationToken());

            _fileStorageService.Verify(x =>
                x.DeleteTempFiles(It.Is<List<DirectoryDownload>>(y =>
                        y.Count == 1 &&
                        y.FirstOrDefault().ServiceName.Equals(_serviceName) &&
                        y.FirstOrDefault().TempPath.Contains(_serviceName)
                    ), It.IsNotNull<CancellationToken>()
                ));
        }

        [Fact]
        public async Task ExecuteAsync_Called_ZipFilesCalledWithProperDirectoryDownloadParameters()
        {
            await _worker.ExecuteAsync(new CancellationToken());

            _fileStorageService.Verify(x =>
                x.ZipFiles(It.Is<List<DirectoryDownload>>(y =>
                        y.Count == 1 &&
                        y.FirstOrDefault().ServiceName.Equals(_serviceName) &&
                        y.FirstOrDefault().TempPath.Contains(_serviceName)
                    ), It.IsNotNull<CancellationToken>()
                ));
        }
    }
}