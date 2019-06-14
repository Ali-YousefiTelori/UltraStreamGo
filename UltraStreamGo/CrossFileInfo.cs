using System;
using System.IO;
using System.Text;

namespace UltraStreamGo
{
    /// <summary>
    /// cross platform file helper
    /// </summary>
    public class CrossFileInfo
    {
        static CrossFileInfo _Current;
        public static CrossFileInfo Current
        {
            get
            {
                if (_Current == null)
                    _Current = new CrossFileInfo();
                return _Current;
            }
            set
            {
                _Current = value;
            }
        }

        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        public virtual Stream Open(string path, FileMode fileMode)
        {
            return File.Open(path, fileMode);
        }

        public virtual Stream Open(string path, FileMode fileMode, FileAccess fileAccess)
        {
            return File.Open(path, fileMode, fileAccess);
        }

        public virtual Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public virtual Stream OpenWrite(string path)
        {
            return File.OpenWrite(path);
        }

        public virtual string ReadAllText(string path, Encoding encoding)
        {
            return File.ReadAllText(path, encoding);
        }

        public virtual byte[] ReadAllBytes(string fileName)
        {
            return System.IO.File.ReadAllBytes(fileName);
        }

        public virtual string[] ReadAllLines(string fileName, Encoding encoding)
        {
            return System.IO.File.ReadAllLines(fileName, encoding);
        }

        public virtual void WriteAllBytes(string fileName, byte[] bytes)
        {
            System.IO.File.WriteAllBytes(fileName, bytes);
        }
        public virtual void WriteAllLines(string fileName, string[] lines, Encoding encoding)
        {
            System.IO.File.WriteAllLines(fileName, lines, encoding);
        }
        public virtual void Move(string oldFile, string newFile)
        {
            System.IO.File.Move(oldFile, newFile);
        }

        public virtual void Copy(string oldFile, string newFile)
        {
            System.IO.File.Copy(oldFile, newFile);
        }

        public virtual void Delete(string fileName)
        {
            System.IO.File.Delete(fileName);
        }

        public virtual Stream Create(string fileName)
        {
            return System.IO.File.Create(fileName);
        }

        public void WriteAllText(string path, string contents, Encoding encoding)
        {
            System.IO.File.WriteAllText(path, contents, encoding);
        }

        public virtual FileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int pageSize)
        {
            return new FileStream(path, mode, access, share, pageSize);
        }

        public virtual FileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int pageSize, FileOptions randomAccess)
        {
            return new FileStream(path, mode, access, share, pageSize, randomAccess);
        }
    }
}
