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
                UploadFileToDataBase();
                

                //StreamIdentifierCache.DefaultCacheFolderPath = "J:\\Test\\Cache";
                //StreamIdentifier.DefaultFolderPath = "J:\\Test\\Data";
                //var checkSum = JsonConvert.DeserializeObject<FileChecksumInfo>(File.ReadAllText("J:\\Test\\chk.chk", Encoding.UTF8)); //FileChecksumMaker.GetFileCheckSum(fileStream);
                ////File.WriteAllText("J:\\Test\\chk.chk", JsonConvert.SerializeObject(checkSum), Encoding.UTF8);
                //using (StreamIdentifierCache stream = new StreamIdentifierCache())
                //{
                //    using (var fileStream = new FileStream("J:\\test.mp4", FileMode.Open, FileAccess.ReadWrite))
                //    {
                //        var guid = Guid.NewGuid();
                //        var uploaded = stream.StartUpload(new FileInfoCache() { DataType = "asa", FileName = "myfile.rar", FileChecksumInfo = checkSum, FileSize = fileStream.Length, Id = guid }, fileStream, 0, fileStream.Length);
                //        StreamIdentifierManager.MoveCacheToFile(guid, 16515616);
                //    }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.ReadKey();
        }


        static void UploadFileToDataBase()
        {
            //your default folder to save files

            StreamIdentifier.DefaultFolderPath = "J:\\Test\\";

            //test file path to save in database
            string filePath = "D:\\SignalGo-Sample.rar";
            //file id from your sql etc id
            int fileId = 456;
            //your file password to access get stream
            string password = "123";
            using (StreamIdentifier streamIdentifier = new StreamIdentifier())
            {
                var info = new System.IO.FileInfo(filePath);
                var checkSum = FileChecksumMaker.GetFileCheckSum(filePath);
                //save your information of file
                FileInfo fileInfo = new FileInfo()
                {
                    Id = fileId,
                    CreatedDateTime = DateTime.Now,
                    FileChecksumInfo = checkSum,
                    FileName = Path.GetFileName(filePath),
                    DataType = "application/x-rar-compressed",
                    FileSize = info.Length,
                    LastUpdateDateTime = DateTime.Now,
                    Password = password
                };

                //start upload your file or stream
                var result = streamIdentifier.StartUpload(fileInfo, filePath, 0, info.Length, (position) =>
                {
                    Console.WriteLine($"Position Wrote : {position} From Length: {info.Length}");
                });

                Console.WriteLine("Upload Result: " + result);
            }

            var fileToDownload = StreamIdentifier.GetFileInfo(fileId, password);
            if (fileToDownload != null)
            {
                //get stream to read 
                var stream = StreamIdentifier.GetFileStream(fileId, 0);
                Console.WriteLine("get file to download success!");
            }
            else
            {
                Console.WriteLine("file not found or password is wrong!");
            }
        }
    }
}
