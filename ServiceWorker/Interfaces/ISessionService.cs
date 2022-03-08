using System.Threading.Tasks;

namespace BackMeUp.ServiceWorker.Interfaces
{
    public interface ISessionService
    {
        /// <summary>
        ///     Contacts the sessions gateway, stores the access token in a cache storage, and returns it.
        /// </summary>
        public Task<string> GetAccessTokenAsync(CancellationToken stoppingTokenbool);

        /// <summary>
        ///     Returns true if an access token is stored inside of the cache storage and the token has not expired yet.
        /// </summary>
        public bool IsValidAccessToken();
    }
}