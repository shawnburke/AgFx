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
using AgFx;

namespace Flickr.Sample.Model {
    public class IdLoadContext :LoadContext {

        public string Id {
            get {
                return (string)Identity;
            }
        }

        public IdLoadContext(string nsid) : base(nsid) {

        }
    }
}
