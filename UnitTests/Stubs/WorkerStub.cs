using BackMeUp.ServiceWorker;
using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BackMeUp.UnitTests.Stubs
{
    /// <summary>
    ///     Due to the implementation of the Worker (inhereting BackgroundService) to test the flow of the Worker it is
    ///     neccessary to create a Mock class
    ///     that exposes ExecuteAsync
    /// </summary>
    public class WorkerStub : Worker
    {
        public WorkerStub(ILogger<Worker> logger, IServiceLocator serviceLocator, CronConfiguration cronConfiguration) :
            base(logger, serviceLocator, cronConfiguration)
        {
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await base.ExecuteAsync(stoppingToken);
        }
    }
}