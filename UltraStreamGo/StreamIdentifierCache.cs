using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UltraStreamGo
{
    public enum StreamIdentifierFileUploadResult : byte
    {
        Success = 1,
        FileCheckSumIsWrong = 2,
        FileSizeIsWrong = 3,
        Exception = 4
    }

    public class StreamIdentifierCache : IDisposable
    {
        public static string DefaultCacheFolderPath { get; set; }
        internal static ConcurrentDictionary<Guid, StreamIdentifierCache> UploadingStreamsByGuid { get; set; } = new ConcurrentDictionary<Guid, StreamIdentifierCache>();
        static object staticLock = new object();

        FileInfoCache CurrentFileInfoCache { get; set; }
        Stream CurrentStream { get; set; }

        internal static string GetCachFolderPath(Guid guid)
        {
            var folders = guid.ToString().Split('-').ToList();
            folders.Insert(0, DefaultCacheFolderPath);
            folders.Insert(0, "Cache");
            folders.Add("0");
            return Path.Combine(folders.ToArray());
        }


        public StreamIdentifierFileUploadResult StartUpload(FileInfoCache fileInfoCache, Stream streamToSave, long startPosition, long length)
        {
            try
            {
                CurrentFileInfoCache = fileInfoCache;
                CurrentStream = streamToSave;
                bool haveToRemove = false;
                StreamIdentifierCache disposeStreamIdentifier = null;
                lock (staticLock)
                {
                    if (UploadingStreamsByGuid.TryGetValue(fileInfoCache.Id, out disposeStreamIdentifier))
                        haveToRemove = true;
                    else
                        UploadingStreamsByGuid.TryAdd(fileInfoCache.Id, this);
                }
                if (haveToRemove)
                    disposeStreamIdentifier.Dispose();

                var folderPath = GetCachFolderPath(fileInfoCache.Id);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, "file");
                var dataPath = Path.Combine(folderPath, "data");
                File.WriteAllText(dataPath, JsonConvert.SerializeObject(fileInfoCache), Encoding.UTF8);
                using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    fileStream.Seek(startPosition, SeekOrigin.Begin);
                    byte[] readBytes = new byte[1024 * 200];
                    long writed = 0;
                    while (!isDisposed)
                    {
                        var readCount = streamToSave.Read(readBytes, 0, readBytes.Length);
                        if (readCount == 0)
                            break;
                        fileStream.Write(readBytes, 0, readCount);
                        writed += readCount;
                        if (writed == length)
                            break;
                    }
                    if (fileStream.Length > fileInfoCache.FileSize)
                        fileStream.SetLength(fileInfoCache.FileSize);
                    if (fileStream.Length == fileInfoCache.FileSize)
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        var checkSum = FileChecksumMaker.GetFileCheckSum(fileStream);
                        var errors = FileChecksumMaker.GetErrorsFromTwoCheckSum(checkSum, fileInfoCache.FileChecksumInfo);
                        if (errors.Count == 0)
                        {
                            return StreamIdentifierFileUploadResult.Success;
                        }
                        else
                            return StreamIdentifierFileUploadResult.FileCheckSumIsWrong;
                    }
                    else
                        return StreamIdentifierFileUploadResult.FileSizeIsWrong;
                }
            }
            catch (Exception ex)
            {
                return StreamIdentifierFileUploadResult.Exception;
            }
            finally
            {
                Dispose();
            }
        }

        bool isDisposed = false;
        public void Dispose()
        {
            isDisposed = true;
            UploadingStreamsByGuid.TryRemove(CurrentFileInfoCache.Id, out StreamIdentifierCache streamIdentifier);
            try
            {
                CurrentStream.Dispose();
            }
            catch (Exception ex)
            {

            }
        }
    }
}
