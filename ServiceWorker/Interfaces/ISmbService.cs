using BackMeUp.ServiceWorker.Models;
using SMBLibrary;
using System;
using System.Collections.Generic;

namespace BackMeUp.ServiceWorker.Interfaces
{
    public interface ISmbService : IDisposable
    {
        /// <summary>
        ///     Queries the SMB file share for the escess backup files.
        /// </summary>
        List<FileDirectoryInformation> GetListOfOldBackups(List<DirectoryDownload> directories, int numberOfBackups);

        /// <summary>
        ///     Deletes a singular file from the SMB file share.
        /// </summary>
        void DeleteFile(FileDirectoryInformation file);

        /// <summary>
        ///     Moves singular file to SMB file share.
        /// </summary>
        /// <param name="directory"></param>
        void MoveFileToShare(DirectoryDownload directory);
    }
}