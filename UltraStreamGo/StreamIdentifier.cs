using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltraStreamGo
{
    public class StreamIdentifier<TId> : IDisposable
    {
        public static string DefaultFolderPath { get; set; }
        internal static ConcurrentDictionary<TId, StreamIdentifier<TId>> UploadingStreamsByIds { get; set; } = new ConcurrentDictionary<TId, StreamIdentifier<TId>>();

        private static readonly object staticLock = new object();
        internal static ConcurrentDictionary<TId, object> UserMemoryCacheByIds { get; set; } = new ConcurrentDictionary<TId, object>();
        public static int PartsLength { get; set; }
        public static string RemoveChars { get; set; }

        private FileInfo<TId> CurrentFileInfo { get; set; }
        public Action DisposedAction { get; set; }
        public bool IgnoreLastUpdateDateTime { get; set; }

        private static Random random = new Random();

        public static bool IsExist(TId fileId)
        {
            string folderPath = GetFolderPath(fileId);
            string dataPath = Path.Combine(folderPath, "data");
            return CrossFileInfo.Current.Exists(dataPath);
        }

        public static bool IsExist(TId fileId, string password)
        {
            string folderPath = GetFolderPath(fileId);
            string dataPath = Path.Combine(folderPath, "data");
            if (!CrossFileInfo.Current.Exists(dataPath))
                return false;
            FileInfo<TId> deserialize = JsonConvert.DeserializeObject<FileInfo<TId>>(CrossFileInfo.Current.ReadAllText(dataPath, Encoding.UTF8));
            if (deserialize.Password != password)
                return false;
            return true;
        }

        public static FileInfo<TId> GetFileInfo(TId fileId, string password = null)
        {
            string folderPath = GetFolderPath(fileId);
            string dataPath = System.IO.Path.Combine(folderPath, "data");
            if (!CrossFileInfo.Current.Exists(dataPath))
                return null;
            FileInfo<TId> deserialize = JsonConvert.DeserializeObject<FileInfo<TId>>(CrossFileInfo.Current.ReadAllText(dataPath, Encoding.UTF8));
            if (deserialize.Password != password)
                return null;
            return deserialize;
        }

        public static string GetRandomString(int count)
        {
            const string chars = "abcdefghijklmmnopqrstuvwxyzABCDEFGIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, count).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GetFilePath(TId fileId)
        {
            string folderPath = GetFolderPath(fileId);
            return System.IO.Path.Combine(folderPath, "file");
        }

        public static System.IO.Stream GetFileStream(TId fileId, long startPosition)
        {
            string folderPath = GetFolderPath(fileId);
            string filePath = System.IO.Path.Combine(folderPath, "file");
            if (!CrossFileInfo.Current.Exists(filePath))
                return null;
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            stream.Seek(startPosition, SeekOrigin.Begin);
            return stream;
        }

        public static bool DeleteFile(TId fileId)
        {
            return StreamIdentifierManager<TId>.DeleteFolder(fileId);
        }

        internal static string GetFolderPath(TId fileId)
        {
            if (string.IsNullOrEmpty(DefaultFolderPath))
                throw new Exception("DefaultFolderPath is not set please set StreamIdentifier.DefaultFolderPath sa your default save folder path");
            List<string> folders = SplitInParts(fileId, PartsLength).ToList();
            folders.Insert(0, DefaultFolderPath);
            folders.Add("0");
            return Path.Combine(folders.ToArray());
        }

        public static IEnumerable<string> SplitInParts(TId data, int partLength)
        {
            var text = data.ToString();
            text = text.Replace(RemoveChars, "");
            for (var i = 0; i < text.Length; i += partLength)
                yield return text.Substring(i, Math.Min(partLength, text.Length - i));
        }

        public Task<StreamIdentifierFileUploadResult> StartUpload(FileInfo<TId> fileInfo, string filePath, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                return StartUpload(fileInfo, fileStream, startPosition, length, wrotePositionAction, trowException);
            }
        }

        public async Task<StreamIdentifierFileUploadResult> StartUpload(FileInfo<TId> fileInfo, Stream streamForRead, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            try
            {
                CurrentFileInfo = fileInfo;
                bool haveToRemove = false;
                StreamIdentifier<TId> disposeStreamIdentifier = null;
                lock (staticLock)
                {
                    if (UploadingStreamsByIds.TryGetValue(fileInfo.Id, out disposeStreamIdentifier))
                        haveToRemove = true;
                    else
                        UploadingStreamsByIds.TryAdd(fileInfo.Id, this);
                }
                if (haveToRemove)
                    disposeStreamIdentifier.Dispose();

                string folderPath = GetFolderPath(fileInfo.Id);

                if (!CrossDirectoryInfo.Current.Exists(folderPath))
                    CrossDirectoryInfo.Current.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, "file");
                string dataPath = Path.Combine(folderPath, "data");
                SaveFileInfoToFile(dataPath, true);
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
                    if (fileStream.Length > fileInfo.FileSize)
                        fileStream.SetLength(fileInfo.FileSize);
                    if (fileStream.Length == fileInfo.FileSize)
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        if (fileInfo.FileChecksumInfo != null)
                        {
                            FileChecksumInfo checkSum = FileChecksumMaker.GetFileCheckSum(fileStream);
                            if (checkSum != null)
                            {
                                List<CheckSumItemInfo> errors = FileChecksumMaker.GetErrorsFromTwoCheckSum(checkSum, fileInfo.FileChecksumInfo);
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

        private void SaveFileInfoToFile(string dataPath, bool isFirstTime)
        {
            if (!isFirstTime || !CrossFileInfo.Current.Exists(dataPath))
                CrossFileInfo.Current.WriteAllText(dataPath, JsonConvert.SerializeObject(CurrentFileInfo), Encoding.UTF8);
        }

        public static void AddObjectToMemoryCache(TId fileId, object value)
        {
            if (fileId == null)
                return;
            UserMemoryCacheByIds[fileId] = value;
        }

        public static bool ExistOnMemoryCache(TId fileId)
        {
            if (fileId == null)
                return false;
            return UserMemoryCacheByIds.ContainsKey(fileId);
        }

        public static bool ExistOnMemoryCache<T>(TId fileId, out T result)
              where T : class
        {
            if (fileId == null)
            {
                result = null;
                return false;
            }
            bool exist = UserMemoryCacheByIds.TryGetValue(fileId, out object value);
            result = (T)value;
            return exist;
        }

        public static T GetValueFromMemoryCache<T>(TId fileId)
            where T : class
        {
            if (fileId == null)
                return null;
            return (T)UserMemoryCacheByIds[fileId];
        }

        public static void RemoveFromMemoryCache(TId fileId)
        {
            if (fileId == null)
                return;
            UserMemoryCacheByIds.TryRemove(fileId, out object value);
        }

        private bool isDisposed = false;
        public void Dispose()
        {
            isDisposed = true;
            if (CurrentFileInfo != null)
                UploadingStreamsByIds.TryRemove(CurrentFileInfo.Id, out StreamIdentifier<TId> streamIdentifier);
            DisposedAction?.Invoke();
        }
    }
}
