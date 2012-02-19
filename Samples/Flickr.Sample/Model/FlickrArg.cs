
namespace Flickr.Sample.Model
{
    public struct FlickrArgument
    {
        public string Name;
        public string Value;

        public FlickrArgument(string n, string v)
        {
            Name = n;
            Value = v;
        }
    }
}
