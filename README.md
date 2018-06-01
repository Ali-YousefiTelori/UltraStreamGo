# UltraStreamGo
Easy to manage file system for save,Upload and download, fast and light


## Features:

1.Caching support
2.File checksum support
3.Password for file support
4.FileShare.ReadWrite for multipe stream read

Sample usage:

```csharp
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
 ```
