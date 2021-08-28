using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UltraStreamGo
{
    /// <summary>
    /// manage pathes of directories
    /// </summary>
    public class DirectoryManager<TId> : IDirectory<TId>
    {
        static DirectoryManager()
        {
            var directoryManager = new DirectoryManager<TId>();
            new StreamIdentifier<TId>(directoryManager);
            Default = directoryManager;
        }

        public static DirectoryManager<TId> Default { get; set; }
        public StreamIdentifier<TId> StreamIdentifier { get; set; }
        public int PartsLength { get; set; } = 3;
        public string RemoveChars { get; set; } = "";
        public string DefaultFolderPath { get; set; }
        public bool IsCaseSensitive { get; set; } = true;
        public bool IgnoreLastUpdateDateTime { get; set; }

        public bool IsExist(TId fileId)
        {
            string folderPath = GetFolderPath(fileId);
            string dataPath = Path.Combine(folderPath, "data");
            return CrossFileInfo.Current.Exists(dataPath);
        }

        public bool IsExist(TId fileId, string password)
        {
            string folderPath = GetFolderPath(fileId);
            string dataPath = Path.Combine(folderPath, "data");
            if (!CrossFileInfo.Current.Exists(dataPath))
                return false;
            FileInfo<TId> deserialize = JsonConvert.DeserializeObject<FileInfo<TId>>(CrossFileInfo.Current.ReadAllText(dataPath, Encoding.UTF8));
            if (IsCaseSensitive)
            {
                if (deserialize.Password != password)
                    return false;
            }
            else
            {
                if (!string.Equals(deserialize.Password, password, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            if (deserialize.IsArchived)
                return false;
            return true;
        }

        public FileInfo<TId> GetFileInfo(TId fileId, string password = null)
        {
            string folderPath = GetFolderPath(fileId);
            string dataPath = System.IO.Path.Combine(folderPath, "data");
            if (!CrossFileInfo.Current.Exists(dataPath))
                return null;
            FileInfo<TId> deserialize = JsonConvert.DeserializeObject<FileInfo<TId>>(CrossFileInfo.Current.ReadAllText(dataPath, Encoding.UTF8));
            if (IsCaseSensitive)
            {
                if (deserialize.Password != password)
                    return null;
            }
            else
            {
                if (!string.Equals(deserialize.Password, password, StringComparison.OrdinalIgnoreCase))
                    return null;
            }
            if (deserialize.IsArchived)
                return null;
            return deserialize;
        }

        public string GetFilePath(TId fileId)
        {
            string folderPath = GetFolderPath(fileId);
            return System.IO.Path.Combine(folderPath, "file");
        }

        public System.IO.Stream GetFileStream(TId fileId, long startPosition)
        {
            string folderPath = GetFolderPath(fileId);
            string filePath = System.IO.Path.Combine(folderPath, "file");
            if (!CrossFileInfo.Current.Exists(filePath))
                return null;
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(startPosition, SeekOrigin.Begin);
            return stream;
        }

        public bool DeleteFile(TId fileId)
        {
            return StreamIdentifierManager<TId>.DeleteFolder(StreamIdentifier, fileId);
        }

        public string GetFolderPath(TId fileId)
        {
            if (string.IsNullOrEmpty(DefaultFolderPath))
                throw new Exception("DefaultFolderPath is not set please set StreamIdentifier.DefaultFolderPath sa your default save folder path");
            List<string> folders = SplitInParts(fileId, PartsLength).ToList();
            folders.Insert(0, DefaultFolderPath);
            folders.Add("0");
            return Path.Combine(folders.ToArray());
        }

        public IEnumerable<string> SplitInParts(TId data, int partLength)
        {
            var text = data.ToString();
            if (!string.IsNullOrEmpty(RemoveChars))
                text = text.Replace(RemoveChars, "");
            for (var i = 0; i < text.Length; i += partLength)
                yield return text.Substring(i, Math.Min(partLength, text.Length - i));
        }

        public bool ArchiveFile(TId fileId, string password = null)
        {
            var fileInfo = GetFileInfo(fileId, password);
            if (fileInfo == null)
                return false;
            fileInfo.IsArchived = true;
            string folderPath = GetFolderPath(fileInfo.Id);
            if (!CrossDirectoryInfo.Current.Exists(folderPath))
                CrossDirectoryInfo.Current.CreateDirectory(folderPath);
            string dataPath = Path.Combine(folderPath, "data");
            CrossFileInfo.Current.WriteAllText(dataPath, JsonConvert.SerializeObject(fileInfo), Encoding.UTF8);
            return true;
        }

        public async Task<StreamIdentifierFileUploadResult> StartUpload(FileInfo<TId> fileInfo, Stream streamForRead, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            try
            {
                string folderPath = GetFolderPath(fileInfo.Id);

                if (!CrossDirectoryInfo.Current.Exists(folderPath))
                    CrossDirectoryInfo.Current.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, "file");
                string dataPath = Path.Combine(folderPath, "data");
                SaveFileInfoToFile(fileInfo, dataPath, true);
                using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
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
                                    SaveFileInfoToFile(fileInfo, dataPath, false);
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
                                SaveFileInfoToFile(fileInfo, dataPath, false);
                                return StreamIdentifierFileUploadResult.Success;
                            }
                        }
                        else
                        {
                            fileInfo.IsComplete = true;
                            if (!IgnoreLastUpdateDateTime)
                                fileInfo.LastUpdateDateTime = DateTime.Now;
                            SaveFileInfoToFile(fileInfo, dataPath, false);
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

            }
        }

        public Task<StreamIdentifierFileUploadResult> StartUpload(FileInfo<TId> fileInfo, string filePath, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                return StartUpload(fileInfo, fileStream, startPosition, length, wrotePositionAction, trowException);
            }
        }

        private void SaveFileInfoToFile(FileInfo<TId> fileInfo, string dataPath, bool isFirstTime)
        {
            if (!isFirstTime || !CrossFileInfo.Current.Exists(dataPath))
                CrossFileInfo.Current.WriteAllText(dataPath, JsonConvert.SerializeObject(fileInfo), Encoding.UTF8);
        }

        public Task<StreamIdentifierFileUploadResult> StartUpload(string fullPath, Stream streamForRead, long startPosition, long length, Action<long> wrotePositionAction = null, bool trowException = false)
        {
            throw new NotSupportedException("DirectoryManager not support fullPath, please call StartUpload(FileInfo<TId> fileInfo...).");
        }
    }
}
