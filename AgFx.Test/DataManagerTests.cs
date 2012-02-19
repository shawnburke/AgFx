using System;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Text;

namespace AgFx.Test {
    [TestClass]
    public class DataManagerTests : WorkItemTest {

        [TestInitialize]
        public void Initialize() {
            DataManager.Current.DeleteCache();
        }

        [TestCleanup]
        public void Cleanup() {
            DataManager.Current.DeleteCache();
        }





        [TestMethod]
        public void TestNoDataLoader() {

            try {
                DataManager.Current.Load<object>("foo");
            }
            catch (InvalidOperationException) {
                return;
            }
            Assert.Fail();
        }


        [TestMethod]
        [Asynchronous]
        public void TestLoad() {
            DataManager.Current.Load<ShortCacheObject>(ShortCacheObject.DefaultIdentifier,
                (obj) =>
                {
                    Assert.AreEqual(ShortCacheObject.DefaultStringValue, obj.StringProp);
                    Assert.AreEqual(ShortCacheObject.DefaultIntValue, obj.IntProp);
                    TestComplete();
                },
                (ex) =>
                {
                    Assert.Fail(ex.Message);
                    TestComplete();
                }
            );
        }

        [TestMethod]
        public void TestLoadFromCache() {

            // populate the cache with an item.
            //
            string uniqueName = CacheEntry.BuildUniqueName(typeof(ShortCacheObject), new LoadContext("LFC"));
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddHours(1));
            DataManager.StoreProvider.Write(cii, ShortCacheObject.SCOLoadRequest.WriteToStream("LoadFromCache", -1).GetBuffer());
            Thread.Sleep(250); // let the write happen;
            // sync load that value.
            //
            var value = DataManager.Current.LoadFromCache<ShortCacheObject>("LFC");

            Assert.AreEqual("LoadFromCache", value.StringProp);
            Assert.AreEqual(-1, value.IntProp);
        }

        [TestMethod]
        [Asynchronous]
        public void TestRefreshBeforeCacheExpires() {
            IUpdatable val = null;
            LoadContext lc = new LoadContext(ShortCacheObject.DefaultIdentifier);
            // write the cache entry
            //
            string uniqueName = CacheEntry.BuildUniqueName(typeof(ShortCacheObject), lc);
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddHours(1));
            var t = DateTime.Now.ToString();
            DataManager.StoreProvider.Write(cii, ShortCacheObject.SCOLoadRequest.WriteToStream(t, -1).GetBuffer());

            val = DataManager.Current.Load<ShortCacheObject>(lc,
                 (v) =>
                 {

                     string oldDefault = ShortCacheObject.DefaultStringValue;
                     // we've got a value
                     Assert.AreEqual(t, v.StringProp);
                     ShortCacheObject.DefaultStringValue = DateTime.Now.ToString();

                     // now request a new value via refresh.
                     //
                     DataManager.Current.Refresh<ShortCacheObject>(ShortCacheObject.DefaultIdentifier,
                         (v2) =>
                         {
                             Assert.AreEqual(v2.StringProp, ShortCacheObject.DefaultStringValue);
                             ShortCacheObject.DefaultStringValue = oldDefault;
                             TestComplete();
                         },
                         (ex2) =>
                         {
                             Assert.Fail(ex2.Message);
                             TestComplete();
                         }

                     );

                 },
                 (ex) =>
                 {
                     Assert.Fail(ex.Message);
                     TestComplete();
                 });
        }

        [TestMethod]
        [Asynchronous]
        public void TestRefreshWithValidCache()
        {
            

            IUpdatable val = null;
            LoadContext lc = new LoadContext(ShortCacheObject.DefaultIdentifier);
            DataManager.Current.Clear<ShortCacheObject>(lc);
            // write the cache entry
            //
            string uniqueName = CacheEntry.BuildUniqueName(typeof(ShortCacheObject), lc);
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddMinutes(1));
            var t = DateTime.Now.ToString();
            DataManager.StoreProvider.Write(cii, ShortCacheObject.SCOLoadRequest.WriteToStream(t, -1).GetBuffer());

            string oldDefault = ShortCacheObject.DefaultStringValue;
            ShortCacheObject.DefaultStringValue = DateTime.Now.Ticks.ToString();        

            val = DataManager.Current.Refresh<ShortCacheObject>(lc,
                 (v) =>
                 {
                    
                     // we've got a value
                     Assert.AreEqual(ShortCacheObject.DefaultStringValue, v.StringProp);
                     ShortCacheObject.DefaultStringValue = oldDefault;
                     TestComplete();
                 },
                 (ex) =>
                 {
                     Assert.Fail(ex.Message);
                     TestComplete();
                 });
        }

        [TestMethod]
        [Asynchronous]
        public void TestInvalidateFromCache() {

            LoadContext lc = new LoadContext("InvalidateFromCache");
            var time = DateTime.Now.ToString();

            // write the cache entry
            //
            string uniqueName = CacheEntry.BuildUniqueName(typeof(ShortCacheObject), lc);
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddHours(1));
            DataManager.StoreProvider.Write(cii, ShortCacheObject.SCOLoadRequest.WriteToStream(time, -1).GetBuffer());

            Thread.Sleep(250);

            TestInvalidateCore(lc, time);

        }

        private void TestInvalidateCore(LoadContext lc, string time) {
            // load it
            //
            DataManager.Current.Load<ShortCacheObject>(lc,
                (sco) =>
                {
                    // verify we got the right thing
                    //
                    Assert.AreEqual(time, sco.StringProp);

                    // load again to verify it's not going to change
                    DataManager.Current.Load<ShortCacheObject>(lc,
                        (sco2) =>
                        {

                            // verify we got the right thing
                            //
                            Assert.AreEqual(time, sco2.StringProp);

                            // invalidate it
                            //
                            DataManager.Current.Invalidate<ShortCacheObject>(lc);

                            Thread.Sleep(250);

                            Assert.AreEqual(time, sco.StringProp);

                            if (time == ShortCacheObject.DefaultStringValue) {
                                ShortCacheObject.DefaultStringValue = "DefaultString";
                            }

                            // load again to verify it's changed.
                            DataManager.Current.Load<ShortCacheObject>(lc,
                                (sco3) =>
                                {

                                    // verify we got the right thing
                                    //
                                    Assert.AreNotEqual(time, sco3.StringProp);

                                    ShortCacheObject.DefaultStringValue = "DefaultString";
                                    TestComplete();

                                },
                                (ex) =>
                                {
                                    Assert.Fail(ex.ToString());
                                    ShortCacheObject.DefaultStringValue = "DefaultString";
                                    TestComplete();
                                });


                        },
                        (ex) =>
                        {
                            Assert.Fail(ex.ToString());
                            ShortCacheObject.DefaultStringValue = "DefaultString";
                            TestComplete();
                        });

                },
                (ex) =>
                {
                    Assert.Fail(ex.ToString());
                    TestComplete();
                }
                );
        }

        [TestMethod]
        [Asynchronous]
        public void TestInvalidateFromLive() {
            LoadContext lc = new LoadContext("InvalidateFromLive");
            DataManager.Current.Clear<ShortCacheObject>(lc);
            var time = DateTime.Now.ToString();

            ShortCacheObject.DefaultStringValue = DateTime.Now.ToString();


            TestInvalidateCore(lc, ShortCacheObject.DefaultStringValue);
        }

        [TestMethod]
        [Asynchronous]
        public void TestRefreshOfLoadFail() {
            ShortCacheObject val = null;

            ShortCacheObject.FailDeserializeMessage = "expected fail.";

            ShortCacheObject.SCOLoadRequest.Error = new InvalidCastException();


            val = DataManager.Current.Load<ShortCacheObject>(ShortCacheObject.DefaultIdentifier,
                 (v) =>
                 {
                     ShortCacheObject.SCOLoadRequest.Error = null;
                     Assert.Fail("Load should have failed.");
                     TestComplete();

                 },
                 (ex) =>
                 {

                     string oldDefault = ShortCacheObject.DefaultStringValue;

                     // we should not get a value.
                     //
                     Assert.AreNotEqual(val.StringProp, oldDefault);
                     ShortCacheObject.DefaultStringValue = DateTime.Now.ToString();

                     ShortCacheObject.FailDeserializeMessage = null;
                     ShortCacheObject.SCOLoadRequest.Error = null;

                     // now request a new value via refresh.
                     //
                     DataManager.Current.Refresh<ShortCacheObject>(ShortCacheObject.DefaultIdentifier,
                         (v2) =>
                         {
                             Assert.AreEqual(v2.StringProp, ShortCacheObject.DefaultStringValue);
                             ShortCacheObject.DefaultStringValue = oldDefault;
                             TestComplete();
                         },
                         (ex2) =>
                         {
                             Assert.Fail(ex2.Message);
                             TestComplete();
                         }

                     );


                 });
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoadWithExpiredCache() {
            string uniqueName = CacheEntry.BuildUniqueName(typeof(ShortCacheObject), new LoadContext("ExpiredItem"));
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddSeconds(-10));
            DataManager.StoreProvider.Write(cii, ShortCacheObject.SCOLoadRequest.WriteToStream("ExpiredItemValue", -1).GetBuffer());
            Thread.Sleep(100); // let the write happen;
            // slow down the load so we get the cache value.
            //
            var oldTime = ShortCacheObject.SCOLoadRequest.LoadTimeMs;
            ShortCacheObject.SCOLoadRequest.LoadTimeMs = 1000;

            DataManager.Current.Load<ShortCacheObject>("ExpiredItem",
                (v) =>
                {
                    ShortCacheObject.SCOLoadRequest.LoadTimeMs = oldTime;
                    Assert.IsNotNull(v);
                    Assert.AreEqual("ExpiredItemValue", v.StringProp);
                    TestComplete();
                },
                (ex) =>
                {
                    Assert.Fail(ex.Message);
                    TestComplete();
                });
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoadRequestError() {
            ShortCacheObject.SCOLoadRequest.Error = new Exception("blah");


            DataManager.Current.Load<ShortCacheObject>("LoadEror",
               (v) =>
               {
                   Assert.Fail("This should have failed");
                   TestComplete();
               },
               (ex) =>
               {
                   Assert.IsNotNull(ex);
                   Assert.AreEqual(ShortCacheObject.SCOLoadRequest.Error, ex.InnerException);
                   ShortCacheObject.SCOLoadRequest.Error = null;
                   TestComplete();
               });
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoadRequestErrorUnhandled() {
            ShortCacheObject.SCOLoadRequest.Error = new Exception("blah");


            EventHandler<System.Windows.ApplicationUnhandledExceptionEventArgs> handler = null;

            handler = (sender, e) =>
            {
                DataManager.Current.UnhandledError -= handler;
                Assert.IsNotNull(e.ExceptionObject);
                Assert.AreEqual(ShortCacheObject.SCOLoadRequest.Error, e.ExceptionObject.InnerException);
                ShortCacheObject.SCOLoadRequest.Error = null;

                PriorityQueue.AddUiWorkItem(() => TestComplete(), true);

                e.Handled = true;
            };

            DataManager.Current.UnhandledError += handler;

            DataManager.Current.Load<ShortCacheObject>("LoadEror",
               (v) =>
               {
                   Assert.Fail("This should have failed");
                   TestComplete();
               },
               null
            );

        }

        [TestMethod]
        [Asynchronous]
        public void TestUpdating() {
            DataManager.Current.Load<ShortCacheObject>("Updating",
                (s) =>
                {
                    Assert.IsFalse(s.IsUpdating);

                    PropertyChangedEventHandler h = null;

                    bool gotUpdatingTrue = false;
                    bool gotUpdatingFalse = false;

                    h = (sender, prop) =>
                    {
                        if (prop.PropertyName == "IsUpdating") {
                            if (!gotUpdatingTrue) {
                                Assert.IsTrue(s.IsUpdating);
                                gotUpdatingTrue = true;
                            }
                            else if (!gotUpdatingFalse) {
                                Assert.IsFalse(s.IsUpdating);
                                gotUpdatingFalse = true;
                                s.PropertyChanged -= h;
                            }
                            else {
                                Assert.Fail();
                            }
                        }

                    };

                    s.PropertyChanged += h;

                    DataManager.Current.Refresh<ShortCacheObject>("Updating",
                        (s2) =>
                        {
                            Assert.IsFalse(s.IsUpdating);
                            if (!gotUpdatingTrue || !gotUpdatingFalse) {
                                Assert.Fail();
                            }

                            TestComplete();
                        },
                        null
                        );

                },
                null
            );
        }

        [TestMethod]
        [Asynchronous]
        public void TestDesrializeFail() {
            string id = "DeserializeFail";
            DataManager.Current.Clear<ShortCacheObject>(id);
            string msg = DateTime.Now.ToString();

            ShortCacheObject.FailDeserializeMessage = msg;
            DataManager.Current.Load<ShortCacheObject>(id,
                (sco) =>
                {
                    ShortCacheObject.FailDeserializeMessage = null;
                    Assert.Fail();
                    TestComplete();
                },
                (ex) =>
                {
                    ShortCacheObject.FailDeserializeMessage = null;
                    Assert.AreEqual(msg, ex.Message);
                    TestComplete();
                }
            );
        }

        [TestMethod]
        [Asynchronous]
        public void TestDeserializeFailUnhandled() {
            string id = "DeserializeFail";
            DataManager.Current.Clear<ShortCacheObject>(id);
            string msg = DateTime.Now.ToString();

            ShortCacheObject.FailDeserializeMessage = msg;

            EventHandler<System.Windows.ApplicationUnhandledExceptionEventArgs> handler = null;

            handler = (sender, e) =>
            {
                DataManager.Current.UnhandledError -= handler;
                ShortCacheObject.FailDeserializeMessage = null;
                Assert.AreEqual(msg, e.ExceptionObject.Message);

                PriorityQueue.AddUiWorkItem(() => TestComplete(), true);

                e.Handled = true;
            };

            DataManager.Current.UnhandledError += handler;

            DataManager.Current.Load<ShortCacheObject>(id,
                (sco) =>
                {
                    ShortCacheObject.FailDeserializeMessage = null;
                    Assert.Fail();
                    TestComplete();
                },
                null
            );
        }

        [TestMethod]
        [Asynchronous]
        public void TestDeserializeCacheFail() {
            string id = "DeserializeCacheFail";

            string uniqueName = CacheEntry.BuildUniqueName(typeof(ShortCacheObject), new LoadContext(id));
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddSeconds(10));

            MemoryStream ms = new MemoryStream();
            var data = Encoding.UTF8.GetBytes("garbage");
            ms.Write(data, 0, data.Length);
            DataManager.StoreProvider.Write(cii, data);

            var val = (int)DateTime.Now.Ticks;

            ShortCacheObject.DefaultIntValue = val;

            DataManager.Current.Load<ShortCacheObject>(id,
                (sco) =>
                {
                    ShortCacheObject.DefaultIntValue = 1234;
                    Assert.AreEqual(val, sco.IntProp);
                    TestComplete();
                },
                (ex) =>
                {

                    Assert.Fail();
                    TestComplete();
                }
            );
        }

        [TestMethod]
        [Asynchronous]
        public void TestDataLoaderLiveLoad() {
            var dval = DateTime.Now.ToString();
            DataManager.Current.Load<TestPoco>(dval,
                 (tp) =>
                 {
                     Assert.AreEqual(dval, tp.Value);
                     TestComplete();
                 },
                 (ex) =>
                 {
                     Assert.Fail();
                     TestComplete();
                 }
            );
        }

        [TestMethod]
        [Asynchronous]
        public void TestCleanup() {

            var dval = DateTime.Now.ToString();
            string uniqueName = CacheEntry.BuildUniqueName(typeof(TestPoco), new LoadContext("foo"));
            DateTime timestamp = DateTime.Now.AddDays(-2);
            var cii = new CacheItemInfo(uniqueName, timestamp, timestamp);
            DataManager.StoreProvider.Write(cii, UTF8Encoding.UTF8.GetBytes(dval));

            DataManager.Current.Cleanup(DateTime.Now.AddDays(-1),
                () =>
                {
                    var item = DataManager.StoreProvider.GetLastestExpiringItem(uniqueName);
                    Assert.IsNull(item);
                    TestComplete();
                }
             );

            
        }
        

        [TestMethod]
        [Asynchronous]
        public void TestDataLoaderCacheLoad() {
            var id = "TestPocoCache";
            var dval = DateTime.Now.ToString();
            string uniqueName = CacheEntry.BuildUniqueName(typeof(TestPoco), new LoadContext(id));
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddSeconds(10));
            DataManager.StoreProvider.Write(cii, UTF8Encoding.UTF8.GetBytes(dval));
            Thread.Sleep(250); // let the write happen;
            DataManager.Current.Load<TestPoco>(id,
                (tp2) =>
                {
                    Assert.AreEqual(dval, tp2.Value);
                    TestComplete();
                },
                (ex) =>
                {
                    Assert.Fail();
                    TestComplete();
                }
           );
        }


        [DataLoader(typeof(TestDataLoader))]
        public class TestPoco {
            public string Value { get; set; }
        }

        public class TestPocoDerived : TestPoco {
            
        }


        public class TestDataLoader : IDataLoader<LoadContext> {
            public static string DeserializeValue = "DefaultDeserializeValue";


            public LoadRequest GetLoadRequest(LoadContext identifier, Type objectType) {
                return new TestDataLoaderRequest(identifier, identifier.Identity.ToString());
            }

            public object Deserialize(LoadContext identifier, Type objectType, Stream stream) {
                StreamReader sr = new StreamReader(stream);

                string val = sr.ReadToEnd();

                if (typeof(TestPoco).IsAssignableFrom(objectType)) {
                    var p = (TestPoco)Activator.CreateInstance(objectType);
                    p.Value = val;
                    return p;
                }

                throw new InvalidOperationException();
            }

            public class TestDataLoaderRequest : LoadRequest {
                string val;

                public TestDataLoaderRequest(LoadContext lc, string v)
                    : base(lc) {
                    val = v;
                }
                public override void Execute(Action<LoadRequestResult> result) {
                    MemoryStream ms = new MemoryStream();

                    StreamWriter sw = new StreamWriter(ms);
                    sw.Write(val);
                    sw.Flush();

                    ms.Seek(0, SeekOrigin.Begin);

                    LoadRequestResult lrr = new LoadRequestResult(ms);
                    result(lrr);
                }
            }
        }

        [TestMethod]
        [Asynchronous]
        public void TestNoCacheObject() {
            var strValue = DateTime.Now.ToString();

            ShortCacheObject.DefaultStringValue = strValue;

            DataManager.Current.Load<NoCacheObject>("nco",
            (v1) =>
            {
                Assert.AreEqual(strValue, v1.StringProp);

                strValue = DateTime.Now.ToString();
                ShortCacheObject.DefaultStringValue = strValue;

                DataManager.Current.Load<NoCacheObject>("nco",
                    (v2) =>
                    {

                        Assert.AreEqual(strValue, v2.StringProp);
                        TestComplete();
                    },
                    (ex2) =>
                    {
                        Assert.Fail();
                        TestComplete();
                    });


            },
            (ex) =>
            {
                Assert.Fail();
                TestComplete();
            });
        }

        [TestMethod]
        [Asynchronous]
        public void TestValidCacheOnlyObjectWithValidCache() {
            var cacheValue = DateTime.Now.ToString();
            var newValue = DateTime.Now.ToString() + "X";

            TestValidCacheCore(cacheValue, newValue, cacheValue, 10);
        }

        [TestMethod]
        [Asynchronous]
        public void TestValidCacheOnlyObjectWithInvalidCache() {
            var cacheValue = DateTime.Now.ToString();
            var newValue = DateTime.Now.ToString() + "X";

            TestValidCacheCore(cacheValue, newValue, newValue, -1);
        }

        private void TestValidCacheCore(string cachedValue, string newValue, string expectedValue, int secondsUntilCacheExpires) {
            string uniqueName = CacheEntry.BuildUniqueName(typeof(ValidCacheOnlyObject), new LoadContext("VCO"));
            var cii = new CacheItemInfo(uniqueName, DateTime.Now, DateTime.Now.AddSeconds(secondsUntilCacheExpires));
            DataManager.StoreProvider.Write(cii, ShortCacheObject.SCOLoadRequest.WriteToStream(cachedValue, -1).GetBuffer());

            Thread.Sleep(100); // sleep to let the write happen;

            ShortCacheObject.DefaultStringValue = newValue;

            DataManager.Current.Load<ValidCacheOnlyObject>("VCO",
            (v1) =>
            {
                Assert.AreEqual(expectedValue, v1.StringProp);
                TestComplete();
            },
            (ex) =>
            {
                Assert.Fail();
                TestComplete();
            });
        }


        [TestMethod]
        [Asynchronous]
        public void TestNestedLoader() {
            string id = "nlo";
            DataManager.Current.Load<TestNestedLoaderObject>(id,
                (val) =>
                {
                    Assert.AreEqual(id, val.StrValue);
                    TestComplete();
                },
                null
            );
        }

        [TestMethod]
        [Asynchronous]
        public void TestRegisterProxy() {
            int propValue = 999;

            ShortCacheObject sco = new ShortCacheObject("Proxy");
            sco.IntProp = propValue;
            Assert.AreNotEqual(propValue, ShortCacheObject.DefaultIntValue);

            DataManager.Current.RegisterProxy<ShortCacheObject>(sco, true,
                (obj) =>
                {
                    Assert.AreNotEqual(propValue, obj.IntProp);
                    Assert.AreEqual(ShortCacheObject.DefaultIntValue, obj.IntProp);
                    Assert.AreEqual(ShortCacheObject.DefaultIntValue, sco.IntProp);
                    TestComplete();
                },
                false
            );

        }

        [TestMethod]
        [Asynchronous]
        public void TestUnusedObjectGC() {
            DataManager.Current.Load<TestPoco>("GC",
                (tp2) =>
                {

                    var entry = DataManager.Current.Get<TestPoco>("GC");

                    PriorityQueue.AddUiWorkItem(

                        () =>
                        {


                            GC.Collect();
                            Assert.IsTrue(entry.HasBeenGCd);
                            TestComplete();
                        });

                    Assert.IsFalse(entry.HasBeenGCd);
                    tp2 = null;
                },
                (ex) =>
                {
                    Assert.Fail();
                    TestComplete();
                }
           );
        }


        [TestMethod]
        [Asynchronous]
        public void TestAutoContextCreation() {

            // verify auto creation of context.
            var obj = DataManager.Current.Load<TestContextObject>(4321,
                (success) =>
                {
                    Assert.IsTrue(success.DeserializedValue.StartsWith(typeof(TestLoadContext).Name));
                    TestComplete();
                },
                (error) =>
                {
                    Assert.Fail(error.Message);
                    TestComplete();
                }
                );



        }


        [TestMethod]
        [Asynchronous]
        public void TestVariableExpirationDefault() {
            var date = default(DateTime);
            var lc = new VariLoadContext(date);
            lc.Foo = 1;

            DataManager.Current.Load<VariableCacheObject>(lc,
                (vco) =>
                {
                    Assert.IsNull(vco.ExpirationTime);

                    // wait a second
                    Thread.Sleep(1000);

                    lc.Foo = 2;                    

                    // do another load - should not come from cache.
                    //
                    DataManager.Current.Load<VariableCacheObject>(lc,
                           (vco2) =>
                           {
                               Assert.IsNull(vco2.ExpirationTime);
                               Assert.AreEqual(lc.Foo, vco2.Foo);

                               TestComplete();
                           },
                           (ex2) =>
                           {
                               Assert.Fail();
                               TestComplete();
                           }
                       );


                },
                (ex) =>
                {
                    Assert.Fail();
                    TestComplete();
                }
            );

        }

      

        [TestMethod]
        [Asynchronous]
        public void TestVariableExpirationTomorrow() {
            var date = DateTime.Now.AddDays(1);
            var lc = new VariLoadContext(date);
            lc.Foo = 1;

            DataManager.Current.Load<VariableCacheObject>(lc,
                (vco) =>
                {
                    Assert.IsNotNull(vco.ExpirationTime);

                    // wait a second
                    Thread.Sleep(1000);

                    lc.Foo = 2;

                    // do another load - SHOULD come from cache.
                    //
                    DataManager.Current.Load<VariableCacheObject>(lc,
                           (vco2) =>
                           {
                               Assert.IsNotNull(vco2.ExpirationTime);
                               Assert.AreNotEqual(lc.Foo, vco2.Foo);
                               Assert.AreEqual(1, vco2.Foo);

                               TestComplete();
                           },
                           (ex2) =>
                           {
                               Assert.Fail();
                               TestComplete();
                           }
                       );

                    
                },
                (ex) =>
                {
                    Assert.Fail();
                    TestComplete();
                }
            );

        }

        [TestMethod]
        [Asynchronous]
        public void TestDerivedClassWithInheritedLoader() {
            var id = DateTime.Now.GetHashCode().ToString();
            var obj = DataManager.Current.Load<TestDerivedNestedLoaderObject>(id,
                (tdnlo) =>
                {
                    Assert.AreEqual(id, tdnlo.StrValue);
                    TestComplete();
                },
                (ex) =>
                {
                    Assert.Fail();
                    TestComplete();
                }
            );
        }

        [TestMethod]
        [Asynchronous]
        public void TestDerivedLoaderAttribute() {

            string id = DateTime.Now.ToString();
            var obj = DataManager.Current.Load<TestPocoDerived>(id,
                (vm) =>
                {
                    Assert.AreEqual(id, vm.Value);
                    TestComplete();
                },
                (ex) =>
                {
                    Assert.Fail();
                    TestComplete();
                }
            );


        }

        public class TestLoadRequest : LoadRequest {

            string _value;

            public TestLoadRequest(LoadContext context, string value)
                : base(context) {
                _value = value;
            }


            public override void Execute(Action<LoadRequestResult> result) {

                MemoryStream str = new MemoryStream(UTF8Encoding.UTF8.GetBytes(_value));
                str.Seek(0, SeekOrigin.Begin);
                result(new LoadRequestResult(str));
            }
        }

        public class TestLoadContext : LoadContext {

            public TestLoadContext(int intCtor)
                : this(intCtor.ToString()) {

            }

            public TestLoadContext(string str)
                : base(typeof(TestLoadContext).Name + ":" + str) {
            }

            public int Option { get; set; }

            protected override string GenerateKey() {
                return string.Format("{0}_{1}", Identity, Option);
            }
        }

        public class TestContextObject : ModelItemBase<TestLoadContext> {

            public TestContextObject() {

            }

            public string DeserializedValue { get; set; }

            public class TestContextDataLoader : IDataLoader<TestLoadContext> {
                public LoadRequest GetLoadRequest(TestLoadContext loadContext, Type objectType) {
                    return new TestLoadRequest(loadContext, String.Format("{0}.{1}", loadContext.Identity, loadContext.Option));
                }

                public object Deserialize(TestLoadContext loadContext, Type objectType, Stream stream) {

                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);

                    string val = UTF8Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                    var tco = new TestContextObject();
                    tco.LoadContext = loadContext;
                    tco.DeserializedValue = val;
                    return tco;

                }
            }

        }

        public class TestNestedLoaderObject {
            public string StrValue { get; set; }

            public class NLO_Loader : IDataLoader<LoadContext> {

                public LoadRequest GetLoadRequest(LoadContext identifier, Type objectType) {
                    return new NestedLoadRequest(identifier);
                }

                public object Deserialize(LoadContext identifier, Type objectType, Stream stream) {
                    StreamReader sr = new StreamReader(stream);

                    var tnlo = (TestNestedLoaderObject)Activator.CreateInstance(objectType);

                    tnlo.StrValue = sr.ReadToEnd();

                    return tnlo;

                }

                private class NestedLoadRequest : LoadRequest {
                    public NestedLoadRequest(LoadContext id)
                        : base(id) {

                    }

                    public override void Execute(Action<LoadRequestResult> result) {
                        MemoryStream ms = new MemoryStream(UTF8Encoding.UTF8.GetBytes(LoadContext.Identity.ToString()));
                        ms.Seek(0, SeekOrigin.Begin);
                        result(new LoadRequestResult(ms));
                    }
                }
            }

        }

        public class TestDerivedNestedLoaderObject : TestNestedLoaderObject {

            
        }        
       

        [CachePolicy(CachePolicy.NoCache)]
        public class NoCacheObject : ShortCacheObject {

            public NoCacheObject() {

            }

            public NoCacheObject(object id)
                : base(id) {
            }

            public class NCODataLoader : ShortCacheObject.SCODataLoader {
                protected override ShortCacheObject CreateInstance(object id) {
                    return new NoCacheObject(id);
                }
            }
        }

        [CachePolicy(CachePolicy.ValidCacheOnly, 10)]
        public class ValidCacheOnlyObject : ShortCacheObject {
            public ValidCacheOnlyObject() {

            }

            public ValidCacheOnlyObject(object id)
                : base(id) {
            }

            public class VCODataLoader : ShortCacheObject.SCODataLoader {
                protected override ShortCacheObject CreateInstance(object id) {
                    return new ValidCacheOnlyObject(id);
                }
            }

        }

        public class VariLoadContext : LoadContext {
            public int Foo { get; set; }
            public VariLoadContext(object id)
                : base(id) {
            }
        }

        [CachePolicy(CachePolicy.CacheThenRefresh, 1)]
        public class VariableCacheObject : ModelItemBase<VariLoadContext>, ICachedItem {

            

            DateTime? _expTime;
            public DateTime? ExpirationTime {
                get { return _expTime; }
                set { _expTime = value; }
            }

            public int Foo { get; set; }


            public class VariCacheLoader : IDataLoader<VariLoadContext> {

                public  LoadRequest GetLoadRequest(VariLoadContext loadContext, Type objectType) {
                    return new VariloadRequest(loadContext);
                }

                public  object Deserialize(VariLoadContext loadContext, Type objectType, Stream stream) {

                    VariableCacheObject vco = new VariableCacheObject();
                    vco.LoadContext = loadContext;
                    var date = (DateTime)loadContext.Identity;
                    vco.ExpirationTime = (date == default(DateTime)) ? null : (DateTime?)date;
                    vco.Foo = loadContext.Foo;
                    return vco;

                }

                public class VariloadRequest : LoadRequest {
                    public VariloadRequest(LoadContext lc)
                        : base(lc) {

                    }

                    public override void Execute(Action<LoadRequestResult> result) {
                        string foo = LoadContext.Identity.ToString();
                        MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(foo));

                        result(new LoadRequestResult(ms));
                    }
                }
            }
        }

        [CachePolicy(CachePolicy.CacheThenRefresh, 10)]
        public class ShortCacheObject : ModelItemBase {

            public static string DefaultIdentifier = "x";
            public static string DefaultStringValue = "StringValue";
            public static int DefaultIntValue = 1234;
            public static string FailDeserializeMessage = null;

            #region Property StringProp
            private string _StringProp;
            public string StringProp {
                get {
                    return _StringProp;
                }
                set {
                    if (_StringProp != value) {
                        _StringProp = value;
                        RaisePropertyChanged("StringProp");
                    }
                }
            }
            #endregion



            #region Property IntProp
            private int _IntProp;
            public int IntProp {
                get {
                    return _IntProp;
                }
                set {
                    if (_IntProp != value) {
                        _IntProp = value;
                        RaisePropertyChanged("IntProp");
                    }
                }
            }
            #endregion

            public ShortCacheObject() {

            }

            public ShortCacheObject(object id)
                : base(id) {
            }

            public class SCODataLoader : IDataLoader<LoadContext> {

                protected virtual ShortCacheObject CreateInstance(object identity) {
                    return new ShortCacheObject(identity);
                }

                public LoadRequest GetLoadRequest(LoadContext identity, Type objectType) {
                    return new SCOLoadRequest(identity, DefaultStringValue, DefaultIntValue);
                }

                public static ShortCacheObject Deserialize(ShortCacheObject item, Stream s)
                {
                    StreamReader sr = new StreamReader(s);

                    ShortCacheObject sco = item;
                    sco.StringProp = sr.ReadLine();

                    // this is expected to fail in the DeserializeCacheFail case
                    sco.IntProp = Int32.Parse(sr.ReadLine());
                    return sco;
                }

                public static void Serialize(ShortCacheObject o, Stream s)
                {
                    StreamWriter sw = new StreamWriter(s);
                    sw.WriteLine(o.StringProp);
                    sw.WriteLine(o.IntProp);
                    sw.Flush();
                }

                public object Deserialize(LoadContext id, Type objectType, Stream stream) {

                    if (FailDeserializeMessage != null) {
                        throw new FormatException(FailDeserializeMessage);
                    }
                    StreamReader sr = new StreamReader(stream);

                    ShortCacheObject sco = CreateInstance(id.Identity);
                    sco.StringProp = sr.ReadLine();

                    // this is expected to fail in the DeserializeCacheFail case
                    sco.IntProp = Int32.Parse(sr.ReadLine());
                    return sco;
                }


            }

            public class SCOLoadRequest : LoadRequest {

                public static int LoadTimeMs = 20;
                public static Exception Error = null;

                string s;
                int i;

                public SCOLoadRequest(LoadContext id, string strValue, int intValue)
                    : base(id) {
                    s = strValue;
                    i = intValue;
                }

                public override void Execute(Action<LoadRequestResult> result) {
                    ThreadPool.QueueUserWorkItem((state) =>
                    {
                        Thread.Sleep(LoadTimeMs);
                        string str = s;
                        int intVal = i;
                        MemoryStream ms = WriteToStream(str, intVal);
                        try {
                            if (Error == null) {
                                result(new LoadRequestResult(ms));
                            }
                            else {
                                result(new LoadRequestResult(Error));
                            }
                        }
                        catch {

                        }
                    },
                    null);
                }

                public static MemoryStream WriteToStream(string str, int intVal) {
                    MemoryStream ms = new MemoryStream();
                    StreamWriter sw = new StreamWriter(ms);
                    sw.WriteLine(str);
                    sw.WriteLine(intVal);
                    sw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                }
            }
        }


    }
}