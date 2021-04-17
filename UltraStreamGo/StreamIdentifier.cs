using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltraStreamGo
{
    public class StreamIdentifier<TId>
    {
        public StreamIdentifier(IDirectory<TId> directory)
        {
            DirectoryManager = directory;
            DirectoryManager.StreamIdentifier = this;
        }

        public IDirectory<TId> DirectoryManager { get; set; }
        public Action DisposedAction { get; set; }

        private static Random random = new Random();

       
        public static string GetRandomString(int count)
        {
            const string chars = "abcdefghijklmmnopqrstuvwxyzABCDEFGIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, count).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
