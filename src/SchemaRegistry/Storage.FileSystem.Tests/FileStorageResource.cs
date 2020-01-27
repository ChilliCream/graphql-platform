using System;
using System.Collections.Concurrent;
using System.IO;
using IOFile = System.IO.File;

namespace MarshmallowPie.Storage.FileSystem
{
    public class FileStorageResource : IDisposable
    {
        private ConcurrentBag<string> _tempDirs = new ConcurrentBag<string>();

        public IFileStorage CreateStorage()
        {
            string temp = Path.GetTempFileName();
            Directory.CreateDirectory(temp + ".d");
            _tempDirs.Add(temp);
            return new FileStorage(temp + ".d");
        }

        public void Dispose()
        {
            while (_tempDirs.TryTake(out string path))
            {
               
                try
                {
                    IOFile.Delete(path);
                    Directory.Delete(path + ".d", true);
                }
                catch { }
            }
        }
    }
}
