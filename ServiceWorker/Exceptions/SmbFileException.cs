using SMBLibrary;

namespace BackMeUp.ServiceWorker.Exceptions
{
    public class SmbFileException : SmbException
    {
        public SmbFileException(string message, NTStatus status, FileStatus fileStatus, string path) : base(message,
            status)
        {
            FileStatus = fileStatus;
            Path = path;
        }

        public FileStatus FileStatus { get; set; }
        public string Path { get; set; }
    }
}