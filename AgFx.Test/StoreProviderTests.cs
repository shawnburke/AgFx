using System;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Collections.Generic;

namespace AgFx.Test
{
    [TestClass]
    public class StoreProviderTests : WorkItemTest
    {

        StoreProviderBase _storeProvider;

        IsolatedStorageFile _isoFile;
        private IsolatedStorageFile IsoStore {
            get {

                if (_isoFile == null) {
                    _isoFile = IsolatedStorageFile.GetUserStoreForApplication();
                }
                return _isoFile;
            }
        }


        private IEnumerable<string> GetIsoStoreFiles() {
            return GetIsoStoreFiles(AgFx.IsoStore.HashedIsoStoreProvider.CacheDirectoryPrefix);
        }

        private IEnumerable<string> GetIsoStoreFiles(string root)
        {
        
            string search = Path.Combine(root, "*");

            List<string> files = new List<string>(IsoStore.GetFileNames(search));

            foreach (var d in IsoStore.GetDirectoryNames(search)) {
                files.AddRange(GetIsoStoreFiles(Path.Combine(root, d)));
            }
            return files;
        }

        private bool CompareFileLists(IEnumerable<string> before, IEnumerable<string> after)
        {
            if (before.Count() != after.Count())
            {
                return false;
            }

            before = before.OrderBy(s => s, StringComparer.InvariantCulture);
            after = after.OrderBy(s => s, StringComparer.InvariantCulture);

            for (int i = 0; i < before.Count(); i++)
            {
                if (!String.Equals(before.ElementAt(i), after.ElementAt(i)))
                {
                    return false;
                }
            }
            return true;

        }


        [TestInitialize]
        public void InitTests()
        {
            _storeProvider = DataManager.StoreProvider;
        }
        
        [TestMethod] 
        public void TestFlush() {


            _storeProvider.Flush(true);
            var files = GetIsoStoreFiles();

            CacheItemInfo cii = new CacheItemInfo("KillMeSoon", DateTime.Now, DateTime.Now.AddHours(1));

            _storeProvider.Write(cii, new byte[] { 2 });
            Thread.Sleep(100); // let the write happen;

            IEnumerable<string> files2 = null;

            if (_storeProvider.IsBuffered) {
                files2 = GetIsoStoreFiles();
                Assert.IsTrue(CompareFileLists(files, files2));

                _storeProvider.Flush(true);

            }
            files2 = GetIsoStoreFiles();
            Assert.IsFalse(CompareFileLists(files, files2));

            _storeProvider.Delete(cii);
            _storeProvider.Flush(true);

        }


        [TestMethod]
        public void TestDelete()
        {
            _storeProvider.Flush(true);

            var files = GetIsoStoreFiles();

            _storeProvider.DeleteAll("KillMe");

            var items = _storeProvider.GetItems("KillMe");

            Assert.AreEqual(0, items.Count());            
            
            CacheItemInfo cii = new CacheItemInfo("KillMe", DateTime.Now, DateTime.Now.AddHours(1));

            _storeProvider.Write(cii, new byte[] { 1 });
            Thread.Sleep(100); // let the write happen;

            items = _storeProvider.GetItems("KillMe");

            Assert.AreEqual(1, items.Count());

            _storeProvider.Delete(cii);

            Thread.Sleep(100);

            items = _storeProvider.GetItems("KillMe");

            Assert.AreEqual(0, items.Count());

            _storeProvider.Flush(true);

            var newFiles = GetIsoStoreFiles();

            Assert.IsTrue(CompareFileLists(files, newFiles));
        }

        [TestMethod]
        public void TestWriteAndReadWithoutFlush()
        {
            var items = _storeProvider.GetItems("KillMe");

            Assert.AreEqual(0, items.Count());

            CacheItemInfo cii = new CacheItemInfo("KillMe", DateTime.Now, DateTime.Now.AddHours(1));

            _storeProvider.Write(cii, new byte[] { 7 });
            Thread.Sleep(100); // let the write happen;

            var bytes = _storeProvider.Read(cii);

            Assert.IsNotNull(bytes);
            Assert.AreEqual(1, bytes.Length);
            Assert.AreEqual(7, bytes[0]);

            // cleanup
            _storeProvider.Delete(cii);
        }

        [TestMethod]
        public void TestWriteAndReadWithFlush()
        {
            _storeProvider.Flush(true);
            var files = GetIsoStoreFiles();

            var items = _storeProvider.GetItems("KillMe");

            Assert.AreEqual(0, items.Count());

            CacheItemInfo cii = new CacheItemInfo("KillMe", DateTime.Now, DateTime.Now.AddHours(1));

            _storeProvider.Write(cii, new byte[] { 7 });
            Thread.Sleep(100); // let the write happen;

            _storeProvider.Flush(true);

            var newFiles = GetIsoStoreFiles();

            Assert.IsFalse(CompareFileLists(files, newFiles));

            var bytes = _storeProvider.Read(cii);

            Assert.IsNotNull(bytes);
            Assert.AreEqual(1, bytes.Length);
            Assert.AreEqual(7, bytes[0]);

            _storeProvider.Delete(cii);
            _storeProvider.Flush(true);

        }

        //[TestMethod]
        //public void TestAutoFlush()
        //{
        //    _storeProvider.Flush(true);
        //    var files = GetIsoStoreFiles();

        //    byte[] bytes = new byte[IsoStore.IsoStoreProvider.FlushThreshholdBytes / 2 + 1];

        //    CacheItemInfo ci1 = new CacheItemInfo("KillMe1", DateTime.Now, DateTime.Now.AddHours(1));
        //    CacheItemInfo ci2 = new CacheItemInfo("KillMe2", DateTime.Now, DateTime.Now.AddHours(1));
            
        //    _storeProvider.Write(ci1, bytes);

        //    var files2 = GetIsoStoreFiles();

        //    Assert.IsTrue(CompareFileLists(files, files2));

        //    _storeProvider.Write(ci2, bytes);

        //    // wait a quarter second for async flush.
        //    //
        //    Thread.Sleep(250);

        //    var files3 = GetIsoStoreFiles();

        //    Assert.IsFalse(CompareFileLists(files, files3));

        //    _storeProvider.Delete(ci1);
        //    _storeProvider.Delete(ci2);

        //    _storeProvider.Flush(true);

        //}
    }
    
}
