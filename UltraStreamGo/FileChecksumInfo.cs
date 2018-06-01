using System;
using System.Collections.Generic;
using System.Text;

namespace UltraStreamGo
{
    public class CheckSumItemInfo
    {
        public string Hash { get; set; }
        public long StartPosition { get; set; }
        public long EndPosition { get; set; }

        public long Size
        {
            get
            {
                return EndPosition - StartPosition;
            }
        }
        public string FileName { get; set; }
    }

    public class FileChecksumInfo
    {
        public List<CheckSumItemInfo> CheckSums { get; set; }
        public int ReadBufferCount { get; set; }
    }
}
