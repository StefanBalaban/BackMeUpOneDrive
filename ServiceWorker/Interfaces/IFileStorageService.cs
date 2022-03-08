using BackMeUp.ServiceWorker.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackMeUp.ServiceWorker.Interfaces
{
    public interface IFileStorageService : IDisposable
    {
        /// <summary>
        ///     Takes the list of directories stored in temp files and zips them.
        /// </summary>
        public void ZipFiles(List<DirectoryDownload> directories, CancellationToken stoppingToken);

        /// <summary>
        ///     Takes the list of zipped temp files and moves them to the designated backup storage location
        /// </summary>
        public void MoveZipFiles(List<DirectoryDownload> directories, CancellationToken stoppingToken);

        /// <summary>
        ///     Deletes the temp directories and zip files.
        /// </summary>
        public void DeleteTempFiles(List<DirectoryDownload> directories, CancellationToken stoppingToken);

        /// <summary>
        ///     Takes the list of services being backed up and,
        ///     with an internal variable that declares the maximum number of backups for each service, deletes the excess backups.
        /// </summary>
        public void DeleteOldBackups(List<DirectoryDownload> directories, CancellationToken stoppingToken);

        /// <summary>
        ///     Creates a directory on the local file system.
        /// </summary>
        void CreateDirectory(string path);

        /// <summary>
        ///     Writes the stream from the FileDownload object to the local file system path in combination with, the objects,
        ///     Location property
        ///     so the structure of the remote cloud storage is followed in the backup.
        /// </summary>
        Task WriteFileToDirectoryAsync(FileDownload fileDownload, string path);
    }
}