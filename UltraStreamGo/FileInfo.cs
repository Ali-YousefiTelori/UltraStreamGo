﻿using System;

namespace UltraStreamGo
{
    public class FileInfo<TId>
    {
        public TId Id { get; set; }
        public string FileName { get; set; }
        public string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(FileName);
            }
        }
        public string DataType { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastUpdateDateTime { get; set; }
        public long FileSize { get; set; }
        public FileChecksumInfo FileChecksumInfo { get; set; }
        public bool IsArchived { get; set; }
        public bool IsComplete { get; set; }
        public string Password { get; set; }
        public bool IsUserDone { get; set; } = true;
    }
}
