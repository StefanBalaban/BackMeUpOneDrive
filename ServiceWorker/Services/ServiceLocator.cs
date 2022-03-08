using BackMeUp.ServiceWorker.Interfaces;

namespace BackMeUp.ServiceWorker.Services
{
    internal class ServiceLocator : IServiceLocator
    {
        private IServiceProvider _serviceProvider;

        public ServiceLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Tuple<IFileStorageService, IEnumerable<IFileDownloadService>> GetServices()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var fileStorageService = scope.ServiceProvider.GetService<IFileStorageService>();
            var fileDownloadServices = scope.ServiceProvider.GetServices<IFileDownloadService>();

            return new Tuple<IFileStorageService, IEnumerable<IFileDownloadService>>(fileStorageService,
                fileDownloadServices);
        }
    }
}