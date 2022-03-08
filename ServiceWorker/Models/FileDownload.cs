namespace BackMeUp.ServiceWorker.Models
{
    public class FileDownload
    {
        public string Id { get; set; }
        public long SizeBytes { get; set; }
        public string Name { get; set; }
        public Stream? Bytes { get; set; }
        public string Path { get; set; }
    }
}