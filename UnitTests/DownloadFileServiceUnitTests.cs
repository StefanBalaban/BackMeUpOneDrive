using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Services;
using BackMeUp.ServiceWorker.Models;
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
    public class DownloadFileServiceUnitTests : IDisposable, IAsyncLifetime
    {
        private OneDriveConfiguration _config;
        private List<FileDownload> _downloadList;
        private Mock<IGraphService> _graphService;
        private Mock<ILogger<OneDriveService>> _logger;
        private OneDriveService _oneDriveService;
        private MemoryStream _stream;

        public async Task DisposeAsync()
        {
            Dispose();
        }

        public async Task InitializeAsync()
        {
            _graphService = new Mock<IGraphService>(MockBehavior.Strict);
            _logger = new Mock<ILogger<OneDriveService>>(MockBehavior.Loose);
            _config = new OneDriveConfiguration {PageSizeInMegabytes = 3};

            byte[]? bytes = new byte[1024];
            new Random().NextBytes(bytes);

            _stream = new MemoryStream(bytes);

            _downloadList = new List<FileDownload>
            {
                new()
                {
                    Bytes = Stream.Null,
                    Id = "1",
                    Name = "A",
                    SizeBytes = 1000000,
                    Path = "X"
                },
                new()
                {
                    Bytes = Stream.Null,
                    Id = "2",
                    Name = "B",
                    SizeBytes = 1000000,
                    Path = "Y"
                },
                new()
                {
                    Bytes = Stream.Null,
                    Id = "3",
                    Name = "C",
                    SizeBytes = 1000000,
                    Path = "Z"
                },
                new()
                {
                    Bytes = Stream.Null,
                    Id = "4",
                    Name = "D",
                    SizeBytes = 1000000,
                    Path = "XY"
                },
                new()
                {
                    Bytes = Stream.Null,
                    Id = "5",
                    Name = "E",
                    SizeBytes = 1000000,
                    Path = "XZ"
                }
            };

            _graphService.Setup(x => x.GetFilesListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_downloadList);

            _graphService.Setup(x => x.DownloadFileAsync(It.IsNotNull<string>(), It.IsNotNull<CancellationToken>()))
                .ReturnsAsync(_stream);


            _oneDriveService = new OneDriveService(_graphService.Object, _config, _logger.Object);
        }

        public void Dispose()
        {
        }

        private static void Write(Stream s, byte[] bytes)
        {
            using (BinaryWriter? writer = new(s))
            {
                writer.Write(bytes);
            }
        }

        [Fact]
        public async Task DownloadFilesAsync_Called_AllDependenciesCalled()
        {
            await _oneDriveService.DownloadFilesAsync(new CancellationToken());

            _graphService.Verify(x => x.GetFilesListAsync(It.IsAny<CancellationToken>()), Times.Once);

            _graphService.Verify(x => x.DownloadFileAsync(It.IsNotNull<string>(), It.IsNotNull<CancellationToken>()),
                Times.Exactly(3));
        }


        [Fact]
        public async Task DownloadFilesAsync_Called_ReturnsThreeDownloadFiles()
        {
            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task DownloadFilesAsync_Called_ReturnEqualToTestData()
        {
            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());

            Assert.Equal(_stream, result.FirstOrDefault().Bytes);

            Assert.Equal(_downloadList.FirstOrDefault().Name, result.FirstOrDefault().Name);
            Assert.Equal(_downloadList.FirstOrDefault().SizeBytes, result.FirstOrDefault().SizeBytes);
            Assert.Equal(_downloadList.FirstOrDefault().Path, result.FirstOrDefault().Path);

            Assert.Equal(_downloadList[1].Name, result[1].Name);
            Assert.Equal(_downloadList[1].SizeBytes, result[1].SizeBytes);
            Assert.Equal(_downloadList[1].Path, result[1].Path);

            Assert.Equal(_downloadList[2].Name, result[2].Name);
            Assert.Equal(_downloadList[2].SizeBytes, result[2].SizeBytes);
            Assert.Equal(_downloadList[2].Path, result[2].Path);
        }

        [Fact]
        public async Task DownloadFilesAsync_CalledWith5MbPageSize_Returns5Results()
        {
            _config.PageSizeInMegabytes = 5;
            _oneDriveService = new OneDriveService(_graphService.Object, _config, _logger.Object);

            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task DownloadFilesAsync_CalledWith10MbPageSize_Returns5Results()
        {
            _config.PageSizeInMegabytes = 10;
            _oneDriveService = new OneDriveService(_graphService.Object, _config, _logger.Object);

            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task DownloadFilesAsync_CalledTwice_Returns5Results()
        {
            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());

            Assert.Equal(3, result.Count);

            result.AddRange(await _oneDriveService.DownloadFilesAsync(new CancellationToken()));
        }

        [Fact]
        public async Task DownloadFilesAsync_CalledTwice_DownloadListInvokedOnce()
        {
            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());
            await _oneDriveService.DownloadFilesAsync(new CancellationToken());


            _graphService.Verify(x => x.GetFilesListAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DownloadFilesAsync_CalledTwice_DownloadFilesInvoked5Times()
        {
            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());
            await _oneDriveService.DownloadFilesAsync(new CancellationToken());


            _graphService.Verify(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(5));
        }

        [Fact]
        public async Task DownloadFilesAsync_CalledTwice_DownloadFilesInvokedWithFileIds()
        {
            var result = await _oneDriveService.DownloadFilesAsync(new CancellationToken());
            await _oneDriveService.DownloadFilesAsync(new CancellationToken());


            _graphService.Verify(
                x => x.DownloadFileAsync(It.Is<string>(y => _downloadList.Select(z => z.Id).Contains(y)),
                    It.IsAny<CancellationToken>()), Times.Exactly(5));
        }
    }
}