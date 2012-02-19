using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Silverlight.Testing;
using System.Threading;
using System;
using System.ComponentModel;

namespace AgFx.Test
{
    [TestClass]
    public class NotifyPropertyChangedBaseTests : WorkItemTest
    {
        [TestMethod]
        public void TestChangedOnUIThread()
        {
            TestChanger tc = new TestChanger(false);
            
            bool gotChange = false;

            tc.PropertyChanged += (s, a) =>
            {
                gotChange = true;
            };

            tc.TestProp = "xyz";

            Assert.IsTrue(gotChange);
        }

        [TestMethod]
        [Asynchronous]
        public void TestChangedOnNonUiThread() {
            TestChanger tc = new TestChanger(true);
            

            var threadId = Thread.CurrentThread.ManagedThreadId;

            PropertyChangedEventHandler handler = null;
            
            handler = (s, a) =>
             {
                 Assert.AreEqual(threadId, Thread.CurrentThread.ManagedThreadId);

                 tc.PropertyChanged -= handler;
                 TestComplete();
             };

            tc.PropertyChanged += handler;

            ThreadPool.QueueUserWorkItem((s) =>
            {
                tc.TestProp = "123";
            }, null);
        }

        [TestMethod]
        [Asynchronous]
        public void TestChangedSynchronousOnUiThread() {
            TestChanger tc = new TestChanger(true);
            
            var threadId = Thread.CurrentThread.ManagedThreadId;

            bool startFirstChagne = false;
            bool finishFirstchange = false;
            bool gotNestedChange = false;

            PropertyChangedEventHandler hander = null;

            hander = (s, a) =>
            {
                bool testComplete = false;

                if (!startFirstChagne) {
                    startFirstChagne = true;
                    tc.TestProp = "again";
                    finishFirstchange = true;
                    if (!gotNestedChange) {
                        Assert.Fail();
                        testComplete = true;
                    }
                }
                else if (!finishFirstchange) {
                    gotNestedChange = true;
                    Assert.AreEqual(threadId, Thread.CurrentThread.ManagedThreadId);
                    testComplete = true;
                }
                else {
                    Assert.Fail();
                    testComplete = true;
                }

                if (testComplete) {
                    tc.PropertyChanged -= hander;
                    TestComplete();
                }
            };

            tc.PropertyChanged += hander;

            ThreadPool.QueueUserWorkItem((s) =>
                {
                    tc.TestProp = "123";
                },
                null
            );
        }

        [TestMethod]
        public void TestDependantProperty() {

            TestChanger tc = new TestChanger();

            bool gotDependantChange = false;

            tc.PropertyChanged += (s, a) =>
            {
                gotDependantChange |= a.PropertyName == "DependentProp";
            };

            tc.TestProp = "changed";

            Assert.IsTrue(gotDependantChange);
        }

        [TestMethod]
        public void TestMultiDependantProperty() {

            TestChanger tc = new TestChanger();

            int notifyCount = 0;
            
            tc.PropertyChanged += (s, a) =>
            {

                if (a.PropertyName == "MultiDependentProp") {
                    notifyCount++;
                }
                
            };

            tc.TestProp = "changed";
            tc.TestProp2 = "changed";

            Assert.AreEqual(2, notifyCount);
        }

        [TestMethod]
        public void TestFakeDependantProperty() {

            TestChanger tc = new TestChanger();

            bool gotDependantChange = false;

            tc.PropertyChanged += (s, a) =>
            {
                gotDependantChange |= a.PropertyName == "FakeDependentProp";
            };

            tc.NotifyFakeProperty("FakeProp");

            Assert.IsTrue(gotDependantChange);
        }

        //[TestMethod]
        //public void TestDeprecatedDependantProperty() {

        //    TestChanger tc = new TestChanger(true);

        //    bool gotDependantChange = false;

        //    tc.PropertyChanged += (s, a) =>
        //    {
        //        gotDependantChange |= a.PropertyName == "NoAttributeDependentProp";
        //    };

        //    tc.TestProp = "changed";

        //    Assert.IsTrue(gotDependantChange);
        //}

        [TestMethod]
        public void TestBadPropertyDependency() {

            try {
                var tc = new TestChanger_Bad();                
            }
            catch (ArgumentException){
                return;
            }
            Assert.Fail();
        }

        public class TestChanger_Bad : NotifyPropertyChangedBase {

            [DependentOnProperty("Nada")]
            public string SomeThing {
                get;
                set;
            }
        
        }

        public class TestChanger : NotifyPropertyChangedBase
        {

            public TestChanger() : this(true) {                
            }

            public TestChanger(bool notifyOnUiThread) : base(notifyOnUiThread){
                //AddDependentProperty("TestProp", "NoAttributeDependentProp");
            }

            #region Property TestProp
            private string _TestProp;
            public string TestProp
            {
                get
                {
                    return _TestProp;
                }
                set
                {
                    if (_TestProp != value)
                    {
                        _TestProp = value;
                        RaisePropertyChanged("TestProp");
                    }
                }
            }
            #endregion

            #region Property TestProp2
            private string _TestProp2;
            public string TestProp2 {
                get {
                    return _TestProp2;
                }
                set {
                    if (_TestProp2 != value) {
                        _TestProp2 = value;
                        RaisePropertyChanged("TestProp2");
                    }
                }
            }
            #endregion

            #region Property TestProp
            private string _dependentProp;

            [DependentOnProperty("TestProp")]
            public string DependentProp {
                get {
                    return _dependentProp;
                }
                set {
                    if (_dependentProp != value) {
                        _dependentProp = value;
                        RaisePropertyChanged("DependentProp");
                    }
                }
            }
            #endregion

            private string _mulltidependentProp;

            [DependentOnProperty("TestProp")]
            [DependentOnProperty("TestProp2")]            
            public string MultiDependentProp {
                get {
                    return _mulltidependentProp;
                }
                set {
                    if (_mulltidependentProp != value) {
                        _mulltidependentProp = value;
                        RaisePropertyChanged("MultiDependentProp");
                    }
                }
            }

            private string _fakeProp;

            [DependentOnProperty(PrimaryPropertyName="FakeProp", IsNotARealPropertyName=true)]
            public string FakeDependentProp {
                get {
                    return _fakeProp;
                }
                set {
                    if (_fakeProp != value) {
                        _fakeProp = value;
                        RaisePropertyChanged("FakeDependentProp");
                    }
                }
            }

            #region Property TestProp
            private string _oldProp;

            [DependentOnProperty("TestProp")]
            public string NoAttributeDependentProp {
                get {
                    return _oldProp;
                }
                set {
                    if (_oldProp != value) {
                        _oldProp = value;
                        RaisePropertyChanged("NoAttributeDependentProp");
                    }
                }
            }
            #endregion

            public void NotifyFakeProperty(string name) {
                RaisePropertyChanged(name);
            }



        }
    }
}
