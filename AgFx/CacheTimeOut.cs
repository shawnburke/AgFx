namespace AgFx
{
    /// <summary>
    /// The base to allow control of cache time based on user calculated values
    /// </summary>
    public abstract class CacheTimeOut
    {
        /// <summary>
        /// The timeout value
        /// </summary>
        public abstract int TimeOutValue { get; }
    }

    /// <summary>
    /// A default version for internal usage
    /// </summary>
    internal class DefaultCacheTimeOut : CacheTimeOut
    {
        private readonly int _getTimeOut;

        public DefaultCacheTimeOut(int timeOut)
        {
            _getTimeOut = timeOut;
        }

        public override int TimeOutValue
        {
            get { return _getTimeOut; }
        }
    }
}