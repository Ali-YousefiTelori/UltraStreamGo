using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace UltraStreamGo
{
    public static class StreamIdentifierManager<TId>
    {
        public static void MoveCacheToFile(StreamIdentifier<TId> streamIdentifier, Guid cacheId, TId fileId)
        {
            var cacheFolder = StreamIdentifierCache.GetCachFolderPath(cacheId);
            var fileCachePath = Path.Combine(cacheFolder, "file");
            var dataCachePath = Path.Combine(cacheFolder, "data");

            var saveFolder = streamIdentifier.DirectoryManager.GetFolderPath(fileId);
            if (CrossDirectoryInfo.Current.Exists(saveFolder))
                throw new Exception("directory exist!");
            CrossDirectoryInfo.Current.CreateDirectory(saveFolder);
            CrossFileInfo.Current.Move(fileCachePath, Path.Combine(saveFolder, "file"));
            var dataCache = JsonConvert.DeserializeObject<FileInfoCache>(CrossFileInfo.Current.ReadAllText(dataCachePath, Encoding.UTF8));

            FileInfo<TId> fileInfo = new FileInfo<TId>()
            {
                CreatedDateTime = DateTime.Now,
                Id = fileId,
                FileName = dataCache.FileName,
                FileSize = dataCache.FileSize,
                IsComplete = true,
                DataType = dataCache.DataType
            };
            CrossFileInfo.Current.WriteAllText(Path.Combine(saveFolder, "data"), JsonConvert.SerializeObject(fileInfo), Encoding.UTF8);
            DeleteFolder(cacheId);
        }

        internal static bool DeleteFolder(Guid cacheId)
        {
            try
            {
                var saveFolder = StreamIdentifierCache.GetCachFolderPath(cacheId);

                while (saveFolder.Length > StreamIdentifierCache.DefaultCacheFolderPath.Length)
                {
                    if (CrossDirectoryInfo.Current.GetDirectories(saveFolder).Length == 0)
                        CrossDirectoryInfo.Current.Delete(saveFolder, true);
                    else
                        break;
                    saveFolder = Path.GetDirectoryName(saveFolder);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        internal static bool DeleteFolder(StreamIdentifier<TId> streamIdentifier, TId fileId)
        {
            try
            {
                var saveFolder = streamIdentifier.DirectoryManager.GetFilePath(fileId);

                while (saveFolder.Length > streamIdentifier.DirectoryManager.DefaultFolderPath.Length)
                {
                    if (CrossDirectoryInfo.Current.GetDirectories(saveFolder).Length == 0)
                        CrossDirectoryInfo.Current.Delete(saveFolder, true);
                    else
                        break;
                    saveFolder = Path.GetDirectoryName(saveFolder);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
