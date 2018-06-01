using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace UltraStreamGo
{
    public class StreamIdentifier : IDisposable
    {
        public static string DefaultFolderPath { get; set; }
        internal static ConcurrentDictionary<long, StreamIdentifier> UploadingStreamsByIds { get; set; } = new ConcurrentDictionary<long, StreamIdentifier>();
        static readonly object staticLock = new object();
        internal static ConcurrentDictionary<long, object> UserMemoryCacheByIds { get; set; } = new ConcurrentDictionary<long, object>();


        FileInfo CurrentFileInfo { get; set; }
        public Action DisposedAction { get; set; }
        public bool IgnoreLastUpdateDateTime { get; set; }
        static Random random = new Random();

        public static FileInfo GetFileInfo(long fileId, string password = null)
        {
            var folderPath = GetFolderPath(fileId);
            var dataPath = Path.Combine(folderPath, "data");
            if (!File.Exists(dataPath))
                return null;
            var deserialize = JsonConvert.DeserializeObject<FileInfo>(File.ReadAllText(dataPath, Encoding.UTF8));
            if (deserialize.Password != password)
                return null;
            return deserialize;
        }

        public static string GetRandomString(int count)
        {
            const string chars = "abcdefghijklmmnopqrstuvwxyzABCDEFGIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, count).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GetFilePath(long fileId)
        {
            var folderPath = GetFolderPath(fileId);
            return Path.Combine(folderPath, "file");
        }

        public static Stream GetFileStream(long fileId, long startPosition)
        {
            var folderPath = GetFolderPath(fileId);
            var filePath = Path.Combine(folderPath, "file");
            if (!File.Exists(filePath))
                return null;
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            stream.Seek(startPosition, SeekOrigin.Begin);
            return stream;
        }

        public static bool DeleteFile(long fileId)
        {
            return StreamIdentifierManager.DeleteFolder(fileId);
        }

        internal static string GetFolderPath(long fileId)
        {
            if (fileId <= 0)
                throw new Exception("Id cannot be zero or lower!");
            var folders = Split(fileId).ToList();
            folders.Insert(0, DefaultFolderPath);
            folders.Add("0");
            return Path.Combine(folders.ToArray());
        }

        static string[] Split(long str)
        {
            return string.Format("{0:n0}", str).Split(',');
        }

        public StreamIdentifierFileUploadResult StartUpload(FileInfo fileInfo, string filePath, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                return StartUpload(fileInfo, fileStream, startPosition, length, wrotePositionAction, trowException);
            }
        }

        public StreamIdentifierFileUploadResult StartUpload(FileInfo fileInfo, Stream streamForRead, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            try
            {
                CurrentFileInfo = fileInfo;
                bool haveToRemove = false;
                StreamIdentifier disposeStreamIdentifier = null;
                lock (staticLock)
                {
                    if (UploadingStreamsByIds.TryGetValue(fileInfo.Id, out disposeStreamIdentifier))
                        haveToRemove = true;
                    else
                        UploadingStreamsByIds.TryAdd(fileInfo.Id, this);
                }
                if (haveToRemove)
                    disposeStreamIdentifier.Dispose();

                var folderPath = GetFolderPath(fileInfo.Id);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                var filePath = Path.Combine(folderPath, "file");
                var dataPath = Path.Combine(folderPath, "data");
                SaveFileInfoToFile(dataPath, true);
                Console.WriteLine($"len {length}");
                using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.Seek(startPosition, SeekOrigin.Begin);
                    byte[] readBytes = new byte[1024 * 512];
                    long writed = 0;
                    while (!isDisposed)
                    {
                        int readCount = 0;
                        try
                        {
                            if (readBytes.Length > length - writed)
                                readBytes = new byte[length - writed];
                            readCount = streamForRead.Read(readBytes, 0, readBytes.Length);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("read exception : " + ex.ToString());
                            break;
                        }
                        if (readCount == 0)
                            break;
                        Console.WriteLine($"write {readBytes.Length} {readCount}");
                        fileStream.Write(readBytes, 0, readCount);
                        writed += readCount;
                        wrotePositionAction?.Invoke(fileStream.Position);
                        if (writed == length)
                            break;
                    }
                    if (fileStream.Length > fileInfo.FileSize)
                        fileStream.SetLength(fileInfo.FileSize);
                    if (fileStream.Length == fileInfo.FileSize)
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        if (fileInfo.FileChecksumInfo != null)
                        {
                            var checkSum = FileChecksumMaker.GetFileCheckSum(fileStream);
                            if (checkSum != null)
                            {
                                var errors = FileChecksumMaker.GetErrorsFromTwoCheckSum(checkSum, fileInfo.FileChecksumInfo);
                                if (errors.Count == 0)
                                {
                                    fileInfo.IsComplete = true;
                                    if (!IgnoreLastUpdateDateTime)
                                        fileInfo.LastUpdateDateTime = DateTime.Now;
                                    SaveFileInfoToFile(dataPath, false);
                                    RemoveFromMemoryCache(fileInfo.Id);
                                    return StreamIdentifierFileUploadResult.Success;
                                }
                                else
                                    return StreamIdentifierFileUploadResult.FileCheckSumIsWrong;
                            }
                            else
                            {
                                fileInfo.IsComplete = true;
                                if (!IgnoreLastUpdateDateTime)
                                    fileInfo.LastUpdateDateTime = DateTime.Now;
                                SaveFileInfoToFile(dataPath, false);
                                RemoveFromMemoryCache(fileInfo.Id);
                                return StreamIdentifierFileUploadResult.Success;
                            }
                        }
                        else
                        {
                            fileInfo.IsComplete = true;
                            if (!IgnoreLastUpdateDateTime)
                                fileInfo.LastUpdateDateTime = DateTime.Now;
                            SaveFileInfoToFile(dataPath, false);
                            RemoveFromMemoryCache(fileInfo.Id);
                            return StreamIdentifierFileUploadResult.Success;
                        }
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
                Dispose();
            }
        }


        void SaveFileInfoToFile(string dataPath, bool isFirstTime)
        {
            if (!isFirstTime || !File.Exists(dataPath))
                File.WriteAllText(dataPath, JsonConvert.SerializeObject(CurrentFileInfo), Encoding.UTF8);
        }

        public static void AddObjectToMemoryCache(long? fileId, object value)
        {
            if (fileId == null)
                return;
            UserMemoryCacheByIds[fileId.Value] = value;
        }

        public static bool ExistOnMemoryCache(long? fileId)
        {
            if (fileId == null)
                return false;
            return UserMemoryCacheByIds.ContainsKey(fileId.Value);
        }

        public static bool ExistOnMemoryCache<T>(long? fileId, out T result)
              where T : class
        {
            if (fileId == null)
            {
                result = null;
                return false;
            }
            var exist = UserMemoryCacheByIds.TryGetValue(fileId.Value, out object value);
            result = (T)value;
            return exist;
        }

        public static T GetValueFromMemoryCache<T>(long? fileId)
            where T : class
        {
            if (fileId == null)
                return null;
            return (T)UserMemoryCacheByIds[fileId.Value];
        }

        public static void RemoveFromMemoryCache(long? fileId)
        {
            if (fileId == null)
                return;
            UserMemoryCacheByIds.TryRemove(fileId.Value, out object value);
        }

        bool isDisposed = false;
        public void Dispose()
        {
            isDisposed = true;
            UploadingStreamsByIds.TryRemove(CurrentFileInfo.Id, out StreamIdentifier streamIdentifier);
            DisposedAction?.Invoke();
        }
    }
}
