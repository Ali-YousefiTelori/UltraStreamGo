using System;
using System.Collections.Generic;
using System.Text;

namespace UltraStreamGo
{
    public class FileInfoCache
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string DataType { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime ExpireDateTime { get; set; }
        public long FileSize { get; set; }
        public bool IsComplete { get; set; }
        public FileChecksumInfo FileChecksumInfo { get; set; }
    }
}
