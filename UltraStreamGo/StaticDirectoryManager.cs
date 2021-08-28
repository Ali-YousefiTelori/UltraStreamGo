using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UltraStreamGo
{
    /// <summary>
    /// manage static files directory manager
    /// </summary>
    public class StaticDirectoryManager : IDirectory<string>
    {
        static StaticDirectoryManager()
        {
            var staticDirectoryManager = new StaticDirectoryManager();
            new StreamIdentifier<string>(staticDirectoryManager);
            Default = staticDirectoryManager;
        }

        public static StaticDirectoryManager Default { get; set; }
        public StreamIdentifier<string> StreamIdentifier { get; set; }
        public int PartsLength { get; set; }
        public string RemoveChars { get; set; } = "";
        public string DefaultFolderPath { get; set; }
        public bool IsCaseSensitive { get; set; } = true;
        public bool IsExist(string fileId)
        {
            string folderPath = GetFilePath(fileId);
            return CrossFileInfo.Current.Exists(folderPath);
        }

        public bool IsExist(string fileId, string password)
        {
            throw new NotSupportedException("Static file not support password, please call IsExist(TId fileId).");
        }

        public FileInfo<string> GetFileInfo(string fileId, string password = null)
        {
            throw new NotSupportedException("Static file not support GetFileInfo, please call GetFileStream(TId fileId, long startPosition).");
        }

        public string GetFilePath(string fileId)
        {
            return GetFolderPath(fileId);
        }

        public System.IO.Stream GetFileStream(string fileId, long startPosition)
        {
            if (!IsExist(fileId))
                return null;
            string filePath = GetFilePath(fileId);
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(startPosition, SeekOrigin.Begin);
            return stream;
        }

        public bool DeleteFile(string fileId)
        {
            try
            {
                var path = GetFilePath(fileId);
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string GetFolderPath(string fileId)
        {
            if (string.IsNullOrEmpty(DefaultFolderPath))
                throw new Exception("DefaultFolderPath is not set please set IDirectory<TId>.DefaultFolderPath sa your default save folder path");
            return Path.Combine(DefaultFolderPath, "StaticFiles", fileId);
        }

        public IEnumerable<string> SplitInParts(string data, int partLength)
        {
            throw new NotSupportedException("Static file not support SplitInParts!");
        }

        public bool ArchiveFile(string fileId, string password = null)
        {
            throw new NotSupportedException("Static file not support ArchiveFile!");
        }


        public async Task<StreamIdentifierFileUploadResult> StartUpload(string fullPath, Stream streamForRead, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            try
            {
                fullPath = GetFilePath(fullPath);
                var directory = Path.GetDirectoryName(fullPath);
                if (!CrossDirectoryInfo.Current.Exists(directory))
                    CrossDirectoryInfo.Current.CreateDirectory(directory);
                using (FileStream fileStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.Seek(startPosition, SeekOrigin.Begin);
                    byte[] readBytes = new byte[1024 * 512];
                    long writed = 0;
                    while (true)
                    {
                        int readCount = 0;
                        try
                        {
                            if (readBytes.Length > length - writed)
                                readBytes = new byte[length - writed];
                            readCount = await streamForRead.ReadAsync(readBytes, 0, readBytes.Length);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("read exception : " + ex.ToString());
                            break;
                        }
                        if (readCount <= 0)
                            break;
                        fileStream.Write(readBytes, 0, readCount);
                        writed += readCount;
                        wrotePositionAction?.Invoke(fileStream.Position);
                        if (writed == length)
                            break;
                    }
                    if (fileStream.Length > length)
                        fileStream.SetLength(length);
                    if (fileStream.Length == length)
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        return StreamIdentifierFileUploadResult.Success;
                    }
                    else
                        return StreamIdentifierFileUploadResult.FileSizeIsWrong;
                }
            }
            catch (Exception ex)
            {
                if (trowException)
                    throw;
                return StreamIdentifierFileUploadResult.Exception;
            }
            finally
            {

            }
        }

        public Task<StreamIdentifierFileUploadResult> StartUpload(FileInfo<string> fileInfo, string filePath, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            throw new NotSupportedException("Static file not support fileInfo, please call StartUpload(string fullPath...).");
        }
    }
}
