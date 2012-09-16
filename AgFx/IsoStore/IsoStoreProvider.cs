// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;

namespace AgFx.IsoStore {

    internal class HashedIsoStoreProvider : StoreProviderBase {
        public const int FlushThreshholdBytes = 100000;

        private const char FileNameSeparator = '»';

        private const string CacheDirectoryName = "«c";

        internal const string CacheDirectoryPrefix = CacheDirectoryName + "\\";

        internal const string CacheDirectorySearchPrefix = CacheDirectoryPrefix + "*";

        private static object LockObject = new object();

        private IsolatedStorageFile _isoFile;

        private IsolatedStorageFile IsoStore {
            get {

                if (_isoFile == null) {
                    _isoFile = IsolatedStorageFile.GetUserStoreForApplication();
                }
                return _isoFile;
            }
        }

        public override bool IsBuffered {
            get { return false; }
        }

        private IEnumerable<string> GetFilesRecursive(string root) {

            string search = Path.Combine(root, "*");

            List<string> files = new List<string>();
            try {
                files.AddRange(IsoStore.GetFileNames(search));
            }
                // These catch statements help with some rare exception stacks
                // similar to this:
                // 
                // System.IO.IsolatedStorage.IsolatedStorageException
                //   at System.IO.IsolatedStorage.IsolatedStorageFile.EnsureAccessToPath(String PathAllowed, String PathRequested)
                //   at System.IO.IsolatedStorage.IsolatedStorageFile.GetFileDirectoryNames(String path, String msg, Boolean file)
                //   at System.IO.IsolatedStorage.IsolatedStorageFile.GetFileNames(String searchPattern)
                //   at AgFx.IsoStore.HashedIsoStoreProvider.GetItems(String uniqueName)
                //   at AgFx.IsoStore.HashedIsoStoreProvider.GetLastestExpiringItem(String uniqueName)
                //   at AgFx.CacheEntry.CacheValueLoader.FindCacheItem()
                //   at AgFx.CacheEntry.CacheValueLoader.get_IsValid()
                //   at AgFx.CacheEntry.LoadInternal(Boolean force)
                //   at AgFx.CacheEntry.<>c__DisplayClass2.<Load>b__0()
                //   at AgFx.PriorityQueue.WorkerThread.<>c__DisplayClass5.<WorkerThreadProc>b__3(Object s)
                //   at System.Threading.ThreadPool.WorkItem.doWork(Object o)
                //   at System.Threading.Timer.ring()
                //
            catch (InvalidOperationException)
            {
            }
            catch (IsolatedStorageException)
            {
            }

            foreach (var d in IsoStore.GetDirectoryNames(search)) {
                files.AddRange(GetFilesRecursive(Path.Combine(root, d)));
            }
            return files;
        }


        public override IEnumerable<CacheItemInfo> GetItems() {

            var files = GetFilesRecursive(CacheDirectoryPrefix);
            var items = from f in files
                        select new FileItem(f).Item;

            return items;

        }

        public override void DeleteAll(string uniqueName) {

            lock (_cache) {
                if (_cache.ContainsKey(uniqueName)) {
                    _cache.Remove(uniqueName);
                }
            }

            // find the directory.
            //
            var dir = FileItem.DirectoryHash(uniqueName);

            if (IsoStore.DirectoryExists(dir)) {
                PriorityQueue.AddStorageWorkItem(() =>
                    {
                        lock (LockObject) {


                            var files = IsoStore.GetFileNames(dir + "\\*");
                            foreach (var f in files) {
                                var path = Path.Combine(dir, f);
                                DeleteFileHelper(IsoStore, path);
                            }
                        }

                    });
            }
        }

        private static void DeleteFileHelper(IsolatedStorageFile isoStore, string path)
        {
            int retries = 3;

            while (retries-- > 0)
            {
                try
                {
                    if (isoStore.FileExists(path))
                    {
                        isoStore.DeleteFile(path);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (IsolatedStorageException)
                {
                    // random iso-store failures..
                    //
                    Thread.Sleep(50);
                }
                return;
            }
        }

        Dictionary<string, CacheItemInfo> _cache = new Dictionary<string, CacheItemInfo>();

        public override IEnumerable<CacheItemInfo> GetItems(string uniqueName) {


            CacheItemInfo item;

            if (_cache.TryGetValue(uniqueName, out item)) {
                return new CacheItemInfo[] { item };
            }

            // find the directory.
            //
            var dir = FileItem.DirectoryHash(uniqueName);


            if (IsoStore.DirectoryExists(dir)) {

                lock (LockObject) {

                    string[] files = null;

                    try
                    {
                        files = IsoStore.GetFileNames(dir + "\\*");
                    }
                    catch (IsolatedStorageException)
                    {
                        // intermittent IsoStore exceptions on shutdown.
                        files = new string[0];
                    }

                    List<CacheItemInfo> items = new List<CacheItemInfo>();

                    foreach (var f in files) {
                        CacheItemInfo cii = FileItem.FromFileName(f);

                        if (cii != null) {
                            items.Add(cii);
                        }
                    }

                    var orderedItems = from i in items
                                       where i.UniqueName == uniqueName
                                       orderby i.ExpirationTime descending
                                       select i;

                    foreach (var i in orderedItems) {
                        if (item == null) {
                            item = i;
                            continue;
                        }

                        Delete(i);
                    }

                    if (item != null) {
                        _cache[uniqueName] = item;
                        return new CacheItemInfo[] { item };
                    }
                }
            }
            return new CacheItemInfo[0];
        }

        public override CacheItemInfo GetLastestExpiringItem(string uniqueName) {
            var items = GetItems(uniqueName);

            return items.FirstOrDefault();
        }


        public override void Flush(bool synchronous) {

        }

        public override void Delete(CacheItemInfo item) {

            CacheItemInfo cachedItem;

            lock (_cache) {
                if (_cache.TryGetValue(item.UniqueName, out cachedItem) && Object.Equals(item, cachedItem)) {
                    _cache.Remove(item.UniqueName);
                }
            }

            var fi = new FileItem(item);

            var fileName = fi.FileName;

            PriorityQueue.AddStorageWorkItem(() =>
                   {
                       lock (LockObject) {
                           DeleteFileHelper(IsoStore, fileName);                           
                       }
                   });
        }

        public override byte[] Read(CacheItemInfo item) {
            var fi = new FileItem(item);
            byte[] bytes = null;

            lock (LockObject) {
                if (!IsoStore.FileExists(fi.FileName)) {
                    return null;
                }

                using (Stream stream = IsoStore.OpenFile(fi.FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                }
            }

            return bytes;
        }

        private const int WriteRetries = 3;

        public override void Write(CacheItemInfo info, byte[] data) {
            var fi = new FileItem(info);            

            PriorityQueue.AddStorageWorkItem(() =>
            {
                lock (LockObject) {

                    for (int r = 0; r < WriteRetries; r++) {
                        try {
                            FileItem.EnsurePath(IsoStore, fi.FileName);
                            using (Stream stream = IsoStore.OpenFile(fi.FileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                                stream.Write(data, 0, data.Length);
                                stream.Flush();
                            }
                            _cache[info.UniqueName] = info;                            
                            break;
                        }
                        catch (IsolatedStorageException ex) {
                            Debug.WriteLine("Exception writing file: Name={0}, Length={1}, {2}", fi.FileName, data.Length, ex.Message);
                            // These IsolatedStorageExceptions seem to happen at random,
                            // haven't yet found a repro.  So for the retry,
                            // if we failed, sleep for a bit and then try again.
                            //
                            // SW: I can repro this with a long unique name - originally thought '/' in the name were the cause
                            Thread.Sleep(50);
                        }
                    }
                }
            });
        }

        public override void Update(CacheItemInfo oldInfo, CacheItemInfo newInfo)
        {
            if (oldInfo.Equals(newInfo)) return;

            var oldFilename = new FileItem(oldInfo).FileName;
            var newFilename = new FileItem(newInfo).FileName;

            PriorityQueue.AddStorageWorkItem(() =>
                {
                    lock (LockObject)
                    {
                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (store.FileExists(newFilename)) 
                                store.DeleteFile(newFilename);

                            using (var readStream = new IsolatedStorageFileStream(oldFilename, FileMode.Open, store))
                            using (var writeStream = new IsolatedStorageFileStream(newFilename, FileMode.Create, store))
                            using (var reader = new StreamReader(readStream))
                            using (var writer = new StreamWriter(writeStream))
                            {
                                writer.Write(reader.ReadToEnd());
                            }

                            store.DeleteFile(oldFilename);
                        }
                    }
                });
        }


        private class FileItem {


            private byte[] _data;
            private string _fileName;
            private string _dirName;
            private CacheItemInfo _item;

            public CacheItemInfo Item {
                get {
                    if (_item == null && _fileName != null) {
                        _item = FromFileName(_fileName);
                    }
                    Debug.Assert(_item != null, "No CacheItemInfo!");
                    return _item;
                }
                private set {
                    _item = value;
                }
            }

            public DateTime WriteTime;

            public byte[] Data {
                get {
                    return _data;
                }
                set {
                    if (_data != value) {
                        _data = value;
                        WriteTime = DateTime.Now;
                    }
                }
            }

            public string DirectoryName {
                get {
                    if (_dirName == null) {
                        _dirName = Item.UniqueName.GetHashCode().ToString();
                    }
                    return _dirName;
                }
            }

            public string FileName {
                get {
                    if (_fileName == null) {
                        _fileName = ToFileName(Item);
                    }
                    return _fileName;
                }
            }

            public int Length {
                get {
                    if (_data == null) {
                        return 0;
                    }
                    return _data.Length;
                }
            }

            public FileItem(string fileName) {
                _fileName = fileName;
            }

            public FileItem(CacheItemInfo item) {
                Item = item;
            }

            public override bool Equals(object obj) {
                var other = (FileItem)obj;

                if (_fileName != null) {
                    return other._fileName == _fileName;
                }
                else if (_item != null) {
                    return Object.Equals(_item, other._item);
                }
                return false;
            }

            public override int GetHashCode() {
                return FileName.GetHashCode();
            }

#if DEBUG

            public override string ToString() {
                return FileName;
            }
#endif

            public static void EnsurePath(IsolatedStorageFile store, string filename) {

                for (string path = Path.GetDirectoryName(filename);
                            path != "";
                            path = Path.GetDirectoryName(path)) {

                    if (!store.DirectoryExists(path)) {
                        store.CreateDirectory(path);
                    }
                }
            }

            public static string DirectoryHash(string uniqueName) {
                return Path.Combine(CacheDirectoryPrefix, uniqueName.GetHashCode().ToString());
            }

            public static CacheItemInfo FromFileName(string fileName) {
                if (!fileName.StartsWith(CacheDirectoryPrefix)) {

                    fileName = Path.GetFileName(fileName);

                    string[] parts = fileName
                        .Split(FileNameSeparator);

                    if (parts.Length == 5)
                    {

                        string uniqueKey = DecodePathName(parts[0]);

                        var item = new CacheItemInfo(uniqueKey) {
                            ExpirationTime = new DateTime(Int64.Parse(parts[2])),
                            UpdatedTime = new DateTime(Int64.Parse(parts[3])),
                            IsOptimized = Boolean.Parse(parts[1]),
                            ETag = DecodePathName(parts[4])
                        };

                        return item;
                    }
                }
                return null;
            }

            private static string DecodePathName(string encodedPath) {
                return Uri.UnescapeDataString(encodedPath);
            }

            private static string EncodePathName(string path) {

                return Uri.EscapeDataString(path);                
            }

            private static string ToFileName(CacheItemInfo item) {
                string name = EncodePathName(item.UniqueName);
                string etag = EncodePathName(item.ETag);
                name = String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}", FileNameSeparator, name, item.IsOptimized, item.ExpirationTime.Ticks, item.UpdatedTime.Ticks, etag);
                name = Path.Combine(DirectoryHash(item.UniqueName), name);
                return name;
            }
        }
    }


}
