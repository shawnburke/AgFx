using System;
using System.Windows;
using AgFx;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;

namespace Flickr.Sample.Model
{

    public abstract class FlickrDataLoaderBase : DefaultDataLoader
    {
        internal static string TryGetValue(XElement parent, string name, string defaultValue, out bool success)
        {
            success = false;
            if (parent == null) return defaultValue;

            var element = parent.Element(name);

            if (element != null)
            {
                success = true;
                return element.Value;

            }

            var attr = parent.Attribute(name);

            if (attr != null)
            {
                success = true;
                return attr.Value;

            }
            return defaultValue;
        }   

        protected WebLoadRequest BuildRequest(LoadContext context, string api, params FlickrArgument[] args)
        {
            string uri = FlickrClient.Current.BuildApiCall(api, true, false, false, args);

            return new WebLoadRequest(context, new Uri(uri));
        }


        public override object Deserialize(LoadContext context, Type objectType, System.IO.Stream stream)
        {
            DateTime start = DateTime.Now;
            
                XElement xElement = XElement.Load(stream);

                Debug.Assert(xElement.Name == "rsp", "XML root is not rsp");

                // check success/fail.
                //
                var stat = xElement.Attribute("stat").Value;

                if (stat != "ok") {
                    var err = xElement.Element("err");

                    var msg = err.Attribute("msg").Value;

                    PriorityQueue.AddUiWorkItem(() =>
                    {

                        MessageBox.Show(String.Format("Error making call {0}: {1}", "getInfo", msg));
                    });



                }
                else {
                    return DeserializeCore(context, (XElement)xElement.FirstNode, objectType, stream);
                }
                return null;

        }

        protected abstract object DeserializeCore(LoadContext context, XElement xml, Type objectType, Stream stream);

    }


   
   
  





   
}
