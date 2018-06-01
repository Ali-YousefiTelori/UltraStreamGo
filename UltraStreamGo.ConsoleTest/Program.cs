using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltraStreamGo.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                StreamIdentifierCache.DefaultCacheFolderPath = "J:\\Test\\Cache";
                StreamIdentifier.DefaultFolderPath = "J:\\Test\\Data";
                var checkSum = JsonConvert.DeserializeObject<FileChecksumInfo>(File.ReadAllText("J:\\Test\\chk.chk", Encoding.UTF8)); //FileChecksumMaker.GetFileCheckSum(fileStream);
                //File.WriteAllText("J:\\Test\\chk.chk", JsonConvert.SerializeObject(checkSum), Encoding.UTF8);
                using (StreamIdentifierCache stream = new StreamIdentifierCache())
                {
                    using (var fileStream = new FileStream("J:\\test.mp4", FileMode.Open, FileAccess.ReadWrite))
                    {
                        var guid = Guid.NewGuid();
                        var uploaded = stream.StartUpload(new FileInfoCache() { DataType = "asa", FileName = "myfile.rar", FileChecksumInfo = checkSum, FileSize = fileStream.Length, Id = guid }, fileStream, 0, fileStream.Length);
                        StreamIdentifierManager.MoveCacheToFile(guid, 16515616);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
