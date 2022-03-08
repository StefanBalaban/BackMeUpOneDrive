namespace BackMeUp.ServiceWorker.Models
{
    public class DirectoryDownload
    {
        private readonly DateTime _dateCreated = DateTime.Now;
        public string ServiceName { get; set; }
        public string TempPath { get; set; }
        public string ZippedPath { get; set; }

        public string GetServiceNameWithDate()
        {
            return ServiceName + ' ' + _dateCreated.ToString("yyyy-MM-dd");
        }
    }
}