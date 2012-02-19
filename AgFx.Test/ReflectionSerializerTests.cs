using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace AgFx.Test
{
    [TestClass]
    public class ReflectionSerializerTests
    {
        
        private TestClass CreateTestClass() {
            return new TestClass
            {
                String = "blah blah blah\r\nblah blah",
                Int = 1234,
                DateTime = new DateTime(1997, 1, 5),
                Bool = true,
                Double = 1234.4321
            };
        }

        [TestMethod]
        public void TestSerialize()
        {
            var tc = CreateTestClass();

            StringWriter sw = new StringWriter();

            ReflectionSerializer.Serialize(tc, sw);

            string data = sw.ToString();


            Assert.AreEqual<string>(_data, data);
        }

        static string _data = "AgFx.Test.ReflectionSerializerTests+TestClass\r\nString:blah%20blah%20blah%0D%0Ablah%20blah\r\nInt:1234\r\nDateTime:01%2F05%2F1997%2000%3A00%3A00\r\nDouble:1234.4321\r\nBool:True\r\n::\r\n";

        [TestMethod]
        public void TestDeserialize()
        {
            TestClass tc = new TestClass();
            TestClass resultClass = CreateTestClass();

            Assert.AreNotEqual(tc, resultClass);

            ReflectionSerializer.Deserialize(tc, new StringReader(_data));

            Assert.AreEqual<TestClass>(resultClass, tc);
        }

        public class TestClass
        {
            public string String { get; set; }
            public int Int { get; set; }
            public DateTime DateTime { get; set; }
            public double Double { get; set; }
            public bool Bool { get; set; }

            public override bool Equals(object obj)
            {
                TestClass other = (TestClass)obj;

                return String == other.String &&
                       Int == other.Int &&
                       Double == other.Double &&
                       DateTime == other.DateTime &&
                       Bool == other.Bool;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

        }
    }
}