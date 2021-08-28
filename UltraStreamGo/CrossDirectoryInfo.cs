using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UltraStreamGo
{
    /// <summary>
    /// cross platform directory helper
    /// </summary>
    public class CrossDirectoryInfo
    {
        static CrossDirectoryInfo _Current;
        public static CrossDirectoryInfo Current
        {
            get
            {
                if (_Current == null)
                    _Current = new CrossDirectoryInfo();
                return _Current;
            }
            set
            {
                _Current = value;
            }
        }
        

        public string[] GetFiles(string path, string searchPattern, SearchOption  searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public virtual void CreateDirectory(string path)
        {
            System.IO.Directory.CreateDirectory(path);
        }

        public virtual void Delete(string path, bool recurcive)
        {
            System.IO.Directory.Delete(path, recurcive);
        }

        public virtual bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        public virtual string[] GetFiles(string sourceFolder)
        {
            return Directory.GetFiles(sourceFolder);
        }

        public virtual string[] GetDirectories(string sourceFolder)
        {
            return Directory.GetDirectories(sourceFolder);
        }
    }
}
