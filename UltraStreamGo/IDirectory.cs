using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UltraStreamGo
{
    public interface IDirectory<TId>
    {
        StreamIdentifier<TId> StreamIdentifier { get; set; }
        string DefaultFolderPath { get; set; }
        bool IsCaseSensitive { get; set; }
        int PartsLength { get; set; }
        string RemoveChars { get; set; }
        string GetFolderPath(TId fileId);
        string GetFilePath(TId fileId);
        bool DeleteFile(TId fileId);
        IEnumerable<string> SplitInParts(TId data, int partLength);
        bool IsExist(TId fileId);
        bool IsExist(TId fileId, string password);
        FileInfo<TId> GetFileInfo(TId fileId, string password = null);
        bool ArchiveFile(TId fileId, string password = null);
        Task<StreamIdentifierFileUploadResult> StartUpload(string fullPath, Stream streamForRead, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false);
        Task<StreamIdentifierFileUploadResult> StartUpload(FileInfo<TId> fileInfo, string filePath, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false);
    }
}
