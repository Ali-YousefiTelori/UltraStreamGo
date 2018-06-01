using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace UltraStreamGo
{
    public static class FileChecksumMaker
    {
        public static Action<long, long> ProgressAction { get; set; }
        public static byte[] GetBytesPerBuffer(Stream stream, int bufferCount)
        {
            List<byte> read = new List<byte>();
            int totalRead = bufferCount;
            while (stream.Position != stream.Length)
            {
                byte[] readBytes = new byte[totalRead];
                int readCount = stream.Read(readBytes, 0, totalRead);
                if (readCount == bufferCount)
                {
                    read.AddRange(readBytes);
                    break;
                }
                else
                {
                    totalRead -= readCount;
                    read.AddRange(readBytes.ToList().GetRange(0, readCount));
                    if (totalRead == 0)
                        break;
                }
            }
            return read.ToArray();
        }

        public static byte[] GetBytesPerBufferNet(Stream stream, int bufferCount, long len, long pos)
        {
            List<byte> read = new List<byte>();
            int totalRead = bufferCount;
            while (true)
            {
                byte[] readBytes = new byte[totalRead];
                int readCount = stream.Read(readBytes, 0, totalRead);
                if (readCount == bufferCount)
                {
                    read.AddRange(readBytes);
                    break;
                }
                else if (readCount == 0)
                {
                    if (read.Count + pos >= len)
                        break;
                }
                else
                {
                    totalRead -= readCount;
                    read.AddRange(readBytes.ToList().GetRange(0, readCount));
                    if (totalRead == 0)
                        break;
                }
            }
            return read.ToArray();
        }

        public static string GetMD5(byte[] data)
        {
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(data);
            string encoded = BitConverter.ToString(hash)
               // without dashes
               .Replace("-", string.Empty)
               // make lowercase
               .ToLower();
            return encoded;
        }

        public static FileChecksumInfo GetFileCheckSum(Stream fileStream, int checkSize = 1024 * 1024)
        {
            FileChecksumInfo data = new FileChecksumInfo() { ReadBufferCount = checkSize };
            List<CheckSumItemInfo> items = new List<CheckSumItemInfo>();
            while (fileStream.Position != fileStream.Length)
            {
                CheckSumItemInfo checkSum = new CheckSumItemInfo();
                checkSum.StartPosition = fileStream.Position;
                var bytes = GetBytesPerBuffer(fileStream, checkSize);
                checkSum.EndPosition = fileStream.Position == fileStream.Length ? fileStream.Length : checkSum.StartPosition + checkSize;
                checkSum.Hash = GetMD5(bytes);
                items.Add(checkSum);
                ProgressAction?.Invoke(fileStream.Position, fileStream.Length);
            }
            data.CheckSums = items;
            return data;
        }

        public static FileChecksumInfo GetFileCheckSum(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return GetFileCheckSum(fileStream);
        }

        public static byte[] GetListOfBytes(Stream stream, long position, int lenght)
        {
            stream.Seek(position, SeekOrigin.Begin);
            var bytes = GetBytesPerBuffer(stream, lenght);
            return bytes;
        }

        //public static void SaveToFile(FileCheckSumData data, string fileName)
        //{
        //    //SerializeStream.SaveSerializeStream(fileName, data);
        //}

        public static List<CheckSumItemInfo> GetErrorsFromTwoCheckSum(FileChecksumInfo downloadData, FileChecksumInfo trueData)
        {
            List<CheckSumItemInfo> items = new List<CheckSumItemInfo>();
            if (trueData.CheckSums.Count != downloadData.CheckSums.Count)
            {
                throw new Exception("Not true");
            }
            for (int i = 0; i < trueData.CheckSums.Count; i++)
            {
                if (trueData.CheckSums[i].Hash != downloadData.CheckSums[i].Hash)
                {
                    if (items.Count > 0 && trueData.CheckSums[i].StartPosition == items.Last().EndPosition)
                    {
                        items.Last().EndPosition = trueData.CheckSums[i].EndPosition;
                    }
                    else
                    {
                        items.Add(trueData.CheckSums[i]);
                    }
                }
            }
            return items;
        }
    }
}
