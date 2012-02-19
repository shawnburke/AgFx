using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Silverlight.Testing;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace AgFx.Test
{
    [TestClass]
    public class BatchObservableCollectionTests : WorkItemTest
    {

        [TestMethod]
        [Asynchronous]
        public void TestAddRange()
        {
            BatchObservableCollection<Foo> foos = new BatchObservableCollection<Foo>(4);

            int changeCount = -1;

            NotifyCollectionChangedEventHandler handler = null;

            handler = (s, a) =>
                {
                    if (changeCount > 0)
                    {
                        foos.CollectionChanged -= handler;

                        TestComplete();
                    }
                    changeCount++;
                };

            foos.CollectionChanged += handler;

            List<Foo> fooList = new List<Foo>();

            for (int i = 0; i < 7; i++)
            {
                fooList.Add(new Foo(i, i.ToString()));
            }
            foos.AddRange(fooList);

            // just make sure the add isn't synhronous
            Assert.AreEqual(-1, changeCount);
        }

        [TestMethod]
        public void TestMerge()
        {
            BatchObservableCollection<Foo> original = new BatchObservableCollection<Foo>(10);

            original.Add(new Foo(1, "1"));
            original.Add(new Foo(2, "1"));
            original.Add(new Foo(3, "1"));
            original.Add(new Foo(5, "1"));
            original.Add(new Foo(7, "1"));
            //original.Add(new Foo(11, "1"));
            //original.Add(new Foo(13, "1"));
            //original.Add(new Foo(17, "1"));


            BatchObservableCollection<Foo> update = new BatchObservableCollection<Foo>(10);

            update.Add(new Foo(1, "_2"));
            update.Add(new Foo(3, "_2"));
            update.Add(new Foo(7, "_2"));
            update.Add(new Foo(9, "2"));
            update.Add(new Foo(10, "2"));
            //update.Add(new Foo(6, "1"));
            //update.Add(new Foo(7, "1"));
            //update.Add(new Foo(12, "1"));
            //update.Add(new Foo(13, "1"));
            //update.Add(new Foo(17, "1"));
            //update.Add(new Foo(23, "1"));
            //update.Add(new Foo(19, "1"));
            //update.Add(new Foo(29, "1"));


            original.CollectionChanged += (s, a) =>
            {
                Assert.AreNotEqual(0, original.Count);
            };

            original.Merge(update, (x, y) => { return x.ID - y.ID; }, EquivelentItemMergeBehavior.ReplaceEqualItems);


            Assert.AreEqual(5, original.Count);

            Assert.AreEqual(original[0].ID, 1);
            Assert.AreEqual(original[0].Value, "_2");

            Assert.AreEqual(original[1].ID, 3);
            Assert.AreEqual(original[1].Value, "_2");

            Assert.AreEqual(original[2].ID, 7);
            Assert.AreEqual(original[2].Value, "_2");

            Assert.AreEqual(original[3].ID, 9);
            Assert.AreEqual(original[3].Value, "2");

            Assert.AreEqual(original[4].ID, 10);
            Assert.AreEqual(original[4].Value, "2");            

        }

        [TestMethod]
        public void TestOffThread()
        {
            ManualResetEvent ev = new ManualResetEvent(false);

            ThreadPool.QueueUserWorkItem((x) =>
            {
                BatchObservableCollection<Foo> bc = new BatchObservableCollection<Foo>(2);

                bc.Add(new Foo());
                bc.Add(new Foo());
                bc.Add(new Foo());

                Thread.Sleep(50);

                bc.Add(new Foo());
                bc.Add(new Foo());
                bc.Add(new Foo());
                bc.Add(new Foo());


                ev.Set();

            },
            null);

            ev.WaitOne();
        }

        public class Foo
        {
            public int ID { get; set; }
            public string Value { get; set; }

            public Foo()
            {

            }

            public Foo(int i, string s)
            {
                ID = i;
                Value = s;
            }

            public override string ToString()
            {
                return string.Format("ID={0}, Value={1}", ID, Value);
            }

            public override bool Equals(object obj)
            {
                Foo other = obj as Foo;
                if (other == null)
                {
                    return false;
                }
                return other.ID == ID;
            }

            public override int GetHashCode()
            {
                return ID.GetHashCode();
            }
        }
    }
}
