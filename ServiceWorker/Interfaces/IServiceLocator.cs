namespace BackMeUp.ServiceWorker.Interfaces
{
    public interface IServiceLocator
    {
        /// <summary>
        ///     Returns a tupple of the services in a single scope
        /// </summary>
        Tuple<IFileStorageService, IEnumerable<IFileDownloadService>> GetServices();
    }
}