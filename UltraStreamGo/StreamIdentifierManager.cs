﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UltraStreamGo
{
    public static class StreamIdentifierManager
    {
        public static void MoveCacheToFile(Guid cacheId, long fileId)
        {
            var cacheFolder = StreamIdentifierCache.GetCachFolderPath(cacheId);
            var fileCachePath = Path.Combine(cacheFolder, "file");
            var dataCachePath = Path.Combine(cacheFolder, "data");

            var saveFolder = StreamIdentifier.GetFolderPath(fileId);
            if (Directory.Exists(saveFolder))
                throw new Exception("directory exist!");
            Directory.CreateDirectory(saveFolder);
            File.Move(fileCachePath, Path.Combine(saveFolder, "file"));
            var dataCache = JsonConvert.DeserializeObject<FileInfoCache>(File.ReadAllText(dataCachePath, Encoding.UTF8));

            FileInfo fileInfo = new FileInfo()
            {
                CreatedDateTime = DateTime.Now,
                Id = fileId,
                FileName = dataCache.FileName,
                FileSize = dataCache.FileSize,
                IsComplete = true,
                DataType = dataCache.DataType
            };
            File.WriteAllText(Path.Combine(saveFolder, "data"), JsonConvert.SerializeObject(fileInfo), Encoding.UTF8);
            DeleteFolder(cacheId);
        }

        internal static bool DeleteFolder(Guid cacheId)
        {
            try
            {
                var saveFolder = StreamIdentifierCache.GetCachFolderPath(cacheId);

                while (saveFolder.Length > StreamIdentifierCache.DefaultCacheFolderPath.Length)
                {
                    if (Directory.GetDirectories(saveFolder).Length == 0)
                        Directory.Delete(saveFolder, true);
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

        internal static bool DeleteFolder(long fileId)
        {
            try
            {
                var saveFolder = StreamIdentifier.GetFolderPath(fileId);

                while (saveFolder.Length > StreamIdentifier.DefaultFolderPath.Length)
                {
                    if (Directory.GetDirectories(saveFolder).Length == 0)
                        Directory.Delete(saveFolder, true);
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
