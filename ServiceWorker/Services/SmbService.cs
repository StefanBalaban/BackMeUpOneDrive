using BackMeUp.ServiceWorker.Configurations;
using BackMeUp.ServiceWorker.Exceptions;
using BackMeUp.ServiceWorker.Interfaces;
using BackMeUp.ServiceWorker.Models;
using SMBLibrary;
using SMBLibrary.Client;
using System.Net;
using FileAttributes = SMBLibrary.FileAttributes;

namespace BackMeUp.ServiceWorker.Services
{
    public class SmbService : ISmbService
    {
        private SMB2Client? _client;
        private ISMBFileStore? _fileStore;
        private ILogger<SmbService> _logger;
        private readonly SmbConfiguration _smbConfiguration;
        private NTStatus _status;
        private bool disposed;

        public SmbService(SmbConfiguration smbConfiguration, ILogger<SmbService> logger)
        {
            _smbConfiguration = smbConfiguration;
            _logger = logger;
        }

        public void DeleteFile(FileDirectoryInformation file)
        {
            if (_client == null || _fileStore == null)
            {
                ConnectToFileShare();
            }

            var filePath = Path.Combine(_smbConfiguration.BackupFolder, file.FileName);

            if (_fileStore is SMB1FileStore)
            {
                filePath = @"\\" + filePath;
            }

            FileStatus fileStatus;
            _status = _fileStore.CreateFile(out object fileHandle, out fileStatus, filePath,
                AccessMask.GENERIC_WRITE | AccessMask.DELETE | AccessMask.SYNCHRONIZE, FileAttributes.Normal,
                ShareAccess.None, CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (_status != NTStatus.STATUS_SUCCESS)
            {
                throw new SmbFileException("Failed to access file that is set to be deleted", _status, fileStatus,
                    filePath);
            }

            FileDispositionInformation fileDispositionInformation = new();
            fileDispositionInformation.DeletePending = true;
            _status = _fileStore.SetFileInformation(fileHandle, fileDispositionInformation);
            bool deleteSucceeded = _status == NTStatus.STATUS_SUCCESS;
            _status = _fileStore.CloseFile(fileHandle);

            if (deleteSucceeded)
            {
                _logger.LogInformation("File {file} has been successfully deleted.", file.FileName);
            }
            else
            {
                _logger.LogWarning("Failed to delete file {file}. NT status: {status}", file.FileName, _status);
            }
        }

        public void MoveFileToShare(DirectoryDownload directory)
        {
            if (_client == null || _fileStore == null)
            {
                ConnectToFileShare();
            }

            string localFilePath = directory.ZippedPath;
            string remoteFilePath =
                Path.Combine(_smbConfiguration.BackupFolder, $"{directory.GetServiceNameWithDate()}.zip");

            if (_fileStore is SMB1FileStore)
            {
                remoteFilePath = @"\\" + remoteFilePath;
            }

            FileStatus fileStatus;

            using var localFileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            _status = _fileStore.CreateFile(out object fileHandle, out fileStatus, remoteFilePath,
                AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None,
                CreateDisposition.FILE_CREATE,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

            if (_status != NTStatus.STATUS_SUCCESS)
            {
                throw new SmbFileException("Failed to create file on share.", _status, fileStatus, remoteFilePath);
            }

            long writeOffset = 0;
            while (localFileStream.Position < localFileStream.Length)
            {
                byte[] buffer = new byte[(int)_client.MaxWriteSize];
                int bytesRead = localFileStream.Read(buffer, 0, buffer.Length);
                if (bytesRead < (int)_client.MaxWriteSize)
                {
                    Array.Resize(ref buffer, bytesRead);
                }

                int numberOfBytesWritten;
                _status = _fileStore.WriteFile(out numberOfBytesWritten, fileHandle, writeOffset, buffer);

                if (_status != NTStatus.STATUS_SUCCESS)
                {
                    throw new SmbException("Failed to write on created file.", _status);
                }

                _logger.LogInformation("File {file} successfully written to file share.", remoteFilePath);

                writeOffset += bytesRead;
            }

            _status = _fileStore.CloseFile(fileHandle);

            if (_status != NTStatus.STATUS_SUCCESS)
            {
                _logger.LogWarning("Acces to file {file} was not successfully closed. NT status: {status}",
                    remoteFilePath, _status);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ConnectToFileShare()
        {
            _client = new SMB2Client();
            bool isConnected = _client.Connect(IPAddress.Parse(_smbConfiguration.StorageAddress),
                SMBTransportType.DirectTCPTransport);
            if (isConnected)
            {
                _status = _client.Login(string.Empty, _smbConfiguration.User, _smbConfiguration.Password);
                if (_status != NTStatus.STATUS_SUCCESS)
                {
                    throw new SmbException("Failed to login to SMB host.", _status);
                }

                _logger.LogInformation("Successfully connected to SMB host.");

                _fileStore = _client.TreeConnect(_smbConfiguration.ShareName, out _status);
                if (_status != NTStatus.STATUS_SUCCESS)
                {
                    throw new SmbException("Failed to connect to share.", _status);
                }

                _logger.LogInformation("Successfully connected to fileshare");
            }
        }

        public List<FileDirectoryInformation> GetListOfOldBackups(List<DirectoryDownload> directories,
            int numberOfBackups)
        {
            if (_client == null || _fileStore == null)
            {
                ConnectToFileShare();
            }

            FileStatus fileStatusRead;
            var filesToDelete = new List<FileDirectoryInformation>();
            List<QueryDirectoryFileInformation> fileList = new List<QueryDirectoryFileInformation>();

            _status = _fileStore.CreateFile(out object directoryHandle, out fileStatusRead,
                _smbConfiguration.BackupFolder, AccessMask.GENERIC_READ, FileAttributes.Directory,
                ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE,
                null);
            if (_status != NTStatus.STATUS_SUCCESS)
            {
                throw new SmbFileException("Failed to read directory", _status, fileStatusRead,
                    _smbConfiguration.BackupFolder);
            }

            _status = _fileStore.QueryDirectory(out fileList, directoryHandle, "*",
                FileInformationClass.FileDirectoryInformation);

            if (_status != NTStatus.STATUS_NO_MORE_FILES)
            {
                _logger.LogWarning("Unexpected status during directory querying. NT status: {status}", _status);
            }

            _status = _fileStore.CloseFile(directoryHandle);

            if (_status != NTStatus.STATUS_SUCCESS)
            {
                _logger.LogWarning("Acces to backup directory was not successfully closed. NT status: {status}",
                    _status);
            }

            // Gather all the files names of the backed up zip files
            foreach (var directory in directories)
            {
                // The file in the directory must match the service name that is being backed up, the file type (.zip), and has to be of an expected length
                var backups = fileList
                    .Where(x =>
                        new string(((FileDirectoryInformation)x).FileName.Take(directory.ServiceName.Length).ToArray())
                            .Equals(directory.ServiceName) &&
                        ((FileDirectoryInformation)x).FileName.EndsWith(".zip") &&
                        ((FileDirectoryInformation)x).FileName.Length == directory.ServiceName.Length + " ".Length +
                        "yyyy-MM-dd".Length + ".zip".Length
                    )
                    .Select(x => (FileDirectoryInformation)x).ToList();

                if (backups.Count > numberOfBackups)
                {
                    filesToDelete.AddRange(backups.OrderByDescending(x => x.CreationTime).Skip(numberOfBackups));
                }
            }

            if (filesToDelete.Count > 0)
            {
                _logger.LogInformation(
                    "Found {number} file(s) for deletion. File name(s): {name}",
                    filesToDelete.Count,
                    string.Join(", ", filesToDelete.Select(x => x.FileName))
                );
            }

            return filesToDelete;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (_fileStore != null)
                    {
                        _fileStore.Disconnect();
                        _fileStore = null;
                    }

                    if (_client != null)
                    {
                        _client.Disconnect();
                        _client = null;
                    }
                }
            }

            disposed = true;
        }
    }
}