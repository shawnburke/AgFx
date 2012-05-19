// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AgFx
{

    /// <summary>
    /// This class does most of the heavy lifting for this framework.
    /// 
    /// The CacheEntry has a few jobs:
    /// 
    /// 1) Allow access to the current value for this item
    /// 2) Know how to get to the cached value for this item
    /// 3) Know how to kick of a new load for this item
    /// 4) Know how to decide which is the current value (cached or new load, based on expiration, etc.)    
    /// </summary>
    internal class CacheEntry : IDisposable
    {
        // some items we use in a bit field to improve instance size.
        //
        private const int SynchronousModeMask =         0x00010000;
        private const int GettingValueMask =            0x00100000;
        private const int UsingCachedValueMask =        0x01000000;
        private const int LoadPendingMask =             0x10000000;
        private const int LiveValueSuccessMask =        0x00001000;
        private const int VersionMask =                 0x00000FFF;
        private int _bitfield;

        // actions pushed in by the DataManager.
        //
        public Func<LoadContext, Stream, bool, object> DeserializeAction { get; set; }
        public Func<object> CreateDefaultAction { get; set; }
        public Func<LiveValueLoader, bool> LoadAction { get; set; }
        public Func<object, Stream, bool> SerializeOptimizedDataAction { get; set; }

        // Completion notifications
        public UpdateCompletionHandler NextCompletedAction { get; private set; }
        private readonly Action<CacheEntry> _proxyComplitionCallback;

#if DEBUG
        public StackTrace LastLoadStackTrace { get; set; }
#endif

        // Object information
        //
        public LoadContext LoadContext { get; set; }
        public Type ObjectType { get; set; }
        private string _uniqueName;
        private TimeSpan? _cacheTime;
        private CachePolicy? _cachePolicy;
        private WeakReference _valueReference = null;
        private object _rootedValue = null;

        // stats
        //
        internal EntryStats _stats;

        // refresh and update state 
        //
        private DateTime _lastUpdatedTime;
        private DateTime _valueExpirationTime = DateTime.MinValue;

        // these guys manage the loading of values from the 
        // cache or from the wire.
        //
        private CacheValueLoader _cacheLoader;
        private LiveValueLoader _liveLoader;

        // we cache policies based on type for perf 
        //
        static Dictionary<Type, CachePolicyAttribute> _cachedPolicies = new Dictionary<Type, CachePolicyAttribute>();

        /// <summary>
        /// The cache policy for this item
        /// </summary>
        public CachePolicy CachePolicy
        {
            get
            {
                EnsureCachePolicy();
                return _cachePolicy.Value;
            }
        }

        /// <summary>
        /// The intended cache lifetime for this item.
        /// </summary>
        public TimeSpan CacheTime
        {
            get
            {
                EnsureCachePolicy();
                return _cacheTime.Value;
            }
        }

        public DateTime ExpirationTime {
            get {
                return _valueExpirationTime;
            }
        }

        // Flag for when value is being fetched.
        //
        private bool GettingValue
        {
            get
            {
                return GetBoolValue(GettingValueMask);
            }
            set
            {
                SetBoolValue(GettingValueMask, value);
            }
        }

        private bool HaveWeEverGottenALiveValue
        {
            get
            {
                return GetBoolValue(LiveValueSuccessMask);
            }
            set {
                SetBoolValue(LiveValueSuccessMask, value);
            }
        }

        /// <summary>
        /// Checks if the current data is valid and should be returned to the caller.
        /// </summary>
        private bool IsDataValid
        {
            get
            {
                if (CachePolicy == AgFx.CachePolicy.NoCache)
                {
                    return false;
                }
                if (GetBoolValue(UsingCachedValueMask) && _cacheLoader.IsValid)
                {
                    return true;
                }
                else if (_valueExpirationTime > DateTime.Now)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// The "version" of the data that's being held.  This is sometimes incremented
        /// to allow calls to know if they've been pre-empted.
        /// </summary>
        private int Version
        {
            get
            {
                return (int)(_bitfield & VersionMask);
            }
            set
            {
                lock (this) {
                    int data = _bitfield & ~VersionMask;

                    _bitfield = data | (value & VersionMask);
                }
            }
        }

        /// <summary>
        /// Makes all load operations happen synchrounously for LoadFromCache
        /// </summary>
        public bool SynchronousMode
        {
            get
            {
                return GetBoolValue(SynchronousModeMask);
            }
            set
            {
                SetBoolValue(SynchronousModeMask, value);
            }
        }

        /// <summary>
        /// The last time this object's data was updated.
        /// </summary>
        private DateTime LastUpdatedTime
        {
            get
            {
                return _lastUpdatedTime;
            }
            set
            {
                _lastUpdatedTime = value;

                UpdateLastUpdated();
            }
        }

        internal static string BuildUniqueName(Type objectType, LoadContext context)
        {

            return string.Format("{0}_{1}", objectType.Name, context.UniqueKey);
        }

        /// <summary>
        /// The unique name for this ObjectType + Identifier combo.
        /// </summary>
        internal string UniqueName
        {
            get
            {
                if (_uniqueName == null)
                {
                    _uniqueName = BuildUniqueName(ObjectType, LoadContext);
                }
                return _uniqueName;
            }
        }

        internal bool HasBeenGCd
        {
            get
            {
                return _valueReference != null && !_valueReference.IsAlive;
            }
        }

        /// <summary>
        /// Gets the object out of the WeakRef, with the ability to surpess any loads, etc.        
        /// </summary>
        /// <param name="load"></param>
        /// <param name="obj"></param>
        private void GetRootedObjectInternal(bool load, out object obj)
        {
            if (_valueReference == null ||
                   !_valueReference.IsAlive)
            {
                // Create a new value if necessary
                //
                bool isNew = _valueReference == null;
                obj = CreateDefaultAction();
                _valueReference = new WeakReference(obj);

                // if it's not new, that means we're resurrecting the object value
                // after it's been GC'd, so reset the state and let the load continue.
                //
                if (!isNew)
                {
                    Debug.WriteLine("A {0} (ID={1}) value has been GC'd, reloading.", ObjectType.Name, LoadContext.Identity);
                    _cacheLoader.Reset();
                    _liveLoader.Reset();
                    SetBoolValue(UsingCachedValueMask, false);                    
                    _valueExpirationTime = DateTime.MinValue;
                    HaveWeEverGottenALiveValue = false;
                }                

                if (load)
                {
                    Load(false);
                }
            }
            else
            {
                // otherwise jut grab the value.
                //
                obj = _valueReference.Target;
            }
        }

        // Retrieves the value without queuing any loads.
        //
        internal object ValueInternal
        {
            get
            {
                object obj;
                GetRootedObjectInternal(false, out obj);
                return obj;
            }
            set {
                if (_valueReference == null) {
                    _valueReference = new WeakReference(value);
                }
            }
        }

        /// <summary>
        /// Retrieve the value and do the right loading stuff 
        /// </summary>
        public object GetValue(bool cacheOnly)
        {
            // do this to prevent AgFx crashing the design surface if called from a 
            // UserControl ctor.
            //
            if (System.ComponentModel.DesignerProperties.IsInDesignTool) {
                return null;
            }

            if (GettingValue)
            {
                return ValueInternal;
            }

            try
            {
                GettingValue = true;
                object value;

                lock (this)
                {
                    GetRootedObjectInternal(false, out value);

                    _stats.OnRequest();

                    if (IsDataValid)
                    {
                        // do nothing, we're done!
                        //
                        NotifyCompletion(null, null);
                    }
                    else if (!cacheOnly)
                    {
                        // Data is out of date or not valid - kick off a new load.
                        //
                        Load(false);
                    }
                }
                Debug.Assert(value != null, "Fail: returning a null value!");
                return value;
            }
            finally
            {
                GettingValue = false;
            }
        }

        /// <summary>
        /// Set up all the handlers for this CacheEntry
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="context"></param>
        /// <param name="proxyCallback">callback that should be invoked when update is finished</param>
        public CacheEntry(LoadContext context, Type objectType, Action<CacheEntry> proxyCallback)
        {
            _proxyComplitionCallback = proxyCallback;
            LoadContext = context;
            ObjectType = objectType;

            _stats = new EntryStats(this);

            // set up our value loaders.
            //
            _cacheLoader = new CacheValueLoader(this);
            _cacheLoader.Loading += ValueLoader_Loading;
            _cacheLoader.ValueAvailable += Cached_ValueAvailable;
            _cacheLoader.LoadFailed += CacheLoader_Failed;

            _liveLoader = new LiveValueLoader(this);
            _liveLoader.Loading += ValueLoader_Loading;
            _liveLoader.ValueAvailable += Live_ValueAvailable;
            _liveLoader.LoadFailed += LiveValueLoader_Failed;

            NextCompletedAction = new UpdateCompletionHandler(this);
        }


        // Helpers for accessing bit field.
        //
        private bool GetBoolValue(int mask)
        {
            return (_bitfield & mask) != 0;
        }

        private void SetBoolValue(int mask, bool value)
        {
            // clear it
            _bitfield &= ~mask;

            if (value)
            {
                _bitfield |= mask;
            }
        }

        // Checks to make sure our value hasn't been cleaned up.
        // if it has, stop doing work because no one is holding the value reference.
        //
        private bool CheckIfAnyoneCares()
        {
            bool doesAnyoneCare = _valueReference != null && _valueReference.IsAlive;

            if (!doesAnyoneCare)
            {
                Debug.WriteLine("Object has been GCd - stopping load.");
            }

            return doesAnyoneCare;
        }

        internal void DoRefresh() {

            if (CheckIfAnyoneCares()) {
                SetForRefresh();
                Load(true);
            }
        }

        // Check to ensure we've loaded the cache policy.
        //
        private void EnsureCachePolicy()
        {
            if (_cachePolicy == null || _cacheTime == null)
            {
                Debug.Assert(ObjectType != null, "Can't get policy before debug type is set.");
                CachePolicyAttribute cpa;
                if (!_cachedPolicies.TryGetValue(ObjectType, out cpa))
                {
                    // check the cache policy
                    //
                    var cpattributes = ObjectType.GetCustomAttributes(typeof(CachePolicyAttribute), true);

                    cpa = (CachePolicyAttribute)cpattributes.FirstOrDefault();

                    if (cpa == null)
                    {
                        cpa = CachePolicyAttribute.Default;
                    }
                    _cachedPolicies[ObjectType] = cpa;
                }

                _cachePolicy = cpa.CachePolicy;

                if (_cachePolicy == CachePolicy.NoCache)
                {
                    _cacheTime = TimeSpan.Zero;
                }
                else
                {
                    _cacheTime = TimeSpan.FromSeconds(cpa.CacheTimeInSeconds);
                }

            }
        }

        /// <summary>
        /// Callback that fires when the LiveValueLoader has completed it's load and is handing
        /// us a new value.
        /// </summary>
        void Live_ValueAvailable(object sender, ValueAvailableEventArgs e)
        {
            // is this the same value we already have?
            //
            if (e.UpdateTime == LastUpdatedTime)
            {
                return;
            }

            // update update and expiration times.
            //
            var value = e.Value;

            ValueLoader loader = (ValueLoader)sender;

            UpdateExpiration(e.UpdateTime, value as ICachedItem);

            if (value != null)
            {
                UpdateFrom(loader, value);
            }
            else
            {
                // not clear what to do with null values.
                //
                NotifyCompletion(loader, null);
                return;
            }

            HaveWeEverGottenALiveValue = true;
            
            // We are no longer using the cached value.
            //
            SetBoolValue(UsingCachedValueMask, false);

            // as long as this thing isn't NoCache, write
            // it to the store.
            //
            if (CachePolicy != AgFx.CachePolicy.NoCache)
            {
                SerializeDataToCache(value, _liveLoader.UpdateTime, _valueExpirationTime, false);
            }
        }

        internal bool SerializeDataToCache(object value, DateTime updateTime, DateTime? expirationTime, bool optimizedOnly) {
            bool isOptimized = false;
            byte[] data = null;

            // see if we can optimize first
            //
            if (SerializeOptimizedDataAction != null) {
                using (MemoryStream outputStream = new MemoryStream()) {
                    if (SerializeOptimizedDataAction(value, outputStream)) {
                        outputStream.Flush();
                        outputStream.Seek(0, SeekOrigin.Begin);
                        var bytes = new byte[outputStream.Length];
                        outputStream.Read(bytes, 0, bytes.Length);
                        isOptimized = true;
                        data = bytes;
                    }                    
                }
            }

            if (optimizedOnly && !isOptimized) {
                return false;
            }

            // oh well, no optimized stream, fall back
            // to normal data.
            if (data == null) {
                data = _liveLoader.Data;
            }

            if (expirationTime == null) {
                expirationTime = updateTime.Add(CacheTime);
            }

            // write the value out to the cache store.
            //
            _cacheLoader.Save(UniqueName, data, updateTime, expirationTime.Value, isOptimized);
            return true;
        }

        private void UpdateExpiration(DateTime lastUpdatedTime, ICachedItem cachedItem) {
            LastUpdatedTime = lastUpdatedTime;

            if (cachedItem == null || cachedItem.ExpirationTime == null) {
                _valueExpirationTime = LastUpdatedTime.Add(CacheTime);
            }
            else {
                _valueExpirationTime = cachedItem.ExpirationTime.Value;
            }

            if (CachePolicy == AgFx.CachePolicy.AutoRefresh) {
                AutoRefreshService.Current.ScheduleRefresh(this);
            }

        }

        /// <summary>
        /// Updates the live value from  an inentional DataManager.Save operation.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="loadContext"></param>
        internal void UpdateValue(object instance, LoadContext loadContext) {

            UpdateExpiration(DateTime.Now, instance as ICachedItem);

            LoadContext = loadContext;           
            if (_valueReference != null && _valueReference.IsAlive) {
                UpdateFrom(null, instance);
            }
            else {
                ValueInternal = instance;
            }
        }

        /// <summary>
        /// The CacheLoader has finished loading and is handing us a new value.
        /// </summary>
        void Cached_ValueAvailable(object sender, ValueAvailableEventArgs e)
        {
            // live loader has data, so we don't care about it anymore.
            //
            if (HaveWeEverGottenALiveValue)
            {
                NextCompletedAction.UnregisterLoader(LoaderType.CacheLoader);
                return;
            }

            // copy the cached value into our state.
            //
            UpdateFrom((ValueLoader)sender, e.Value);
            SetBoolValue(UsingCachedValueMask, true);

            
            UpdateExpiration(e.UpdateTime, e.Value as ICachedItem);
        }

        void CacheLoader_Failed(object s, ExceptionEventArgs e)
        {
            // if the cache load failed, make sure we're doing a live load.
            // if we aren't, kick one off.
            //
            NextCompletedAction.UnregisterLoader(LoaderType.CacheLoader);
            if (!_liveLoader.IsBusy && !HaveWeEverGottenALiveValue)
            {
                NextCompletedAction.RegisterActiveLoader(LoaderType.LiveLoader);
                _liveLoader.FetchData(true);
            }
        }

        void LiveValueLoader_Failed(object s, ExceptionEventArgs e)
        {
            NotifyCompletion((ValueLoader)s, e.Exception);
        }

        void ValueLoader_Loading(object s, EventArgs e)
        {
            IUpdatable iupd = ValueInternal as IUpdatable;

            if (iupd != null)
            {
                iupd.IsUpdating = true;
            }

        }

        /// <summary>
        /// Initiate a load
        /// </summary>
        /// <param name="force">True to always load a new value.</param>
        private void Load(bool force)
        {
            lock (this)
            {
                // someone is already trying to do a load.
                if (GetBoolValue(LoadPendingMask) || _cacheLoader.IsBusy || _liveLoader.IsBusy || !_liveLoader.IsValid)
                {
                    return;
                }
                SetBoolValue(LoadPendingMask, true);

                // root the object value so that it doesn't get GC'd while we're processing.
                //
                GetRootedObjectInternal(false, out _rootedValue);

                PriorityQueue.AddWorkItem(() =>
                    LoadInternal(force)
                );
            }
        }

        /// <summary>
        /// Synchronously load a value from the cache and return it.
        /// </summary>
        /// <returns></returns>
        internal object LoadFromCache()
        {
            try
            {
                SynchronousMode = true;
                _cacheLoader.SynchronousMode = true;

                // only return a valid cache.
                if (_cacheLoader.IsValid)
                {
                    object value = ValueInternal;

                    StartLoading(false, _cacheLoader);
                    return value;
                }
            }
            finally
            {
                _cacheLoader.SynchronousMode = false;
                SynchronousMode = false;
            }
            return null;
        }

        /// <summary>
        /// LoadInternal does the heavy lifting of the load.
        /// </summary>
        /// <param name="force"></param>
        private void LoadInternal(bool force)
        {
            try
            {
                // first to see if we have a valid live value.
                //
                if (!force && DateTime.Now < _valueExpirationTime)
                {
                    // somehow we got in here, so do nothing.
                    //
                    return;
                }
                else if (force || HaveWeEverGottenALiveValue)
                {
                    // we had a value but it expired, kick off a new live load.
                    //
                    if (!_liveLoader.IsBusy)
                    {
                        Debug.WriteLine("{0}: Data for {1} (ID={2}) has expired, reloading.", DateTime.Now, ObjectType.Name, LoadContext.Identity);
                        StartLoading(force, _liveLoader);
                    }
                    return;
                }
                else if (GetBoolValue(UsingCachedValueMask) && !_cacheLoader.IsValid)
                {
                    Debug.WriteLine("{0}: Failed cache load for {1} (ID={2}) reloading live data.", DateTime.Now, ObjectType.Name, LoadContext.Identity);

                    StartLoading(true, _liveLoader);
                    return;
                }
                else if (_cacheLoader.LoadState == DataLoadState.ValueAvailable)
                {
                    // we already loaded the cache and nothing has changed, so do nothing.
                    //                
                    return;
                }

                lock (this)
                {
                    // this is the initial load state, so we check the cache then figure
                    // out what to do.

                    // first check the cache state.
                    //
                    var isCacheValid = _cacheLoader.IsValid;

                    if (!isCacheValid)
                    {
                        // if the cache is NOT valid, figure out what to do about
                        // it based on the cache policy.
                        //

                        // start a live load.
                        StartLoading(false, _liveLoader);


                        switch (CachePolicy)
                        {
                            case CachePolicy.NoCache:
                            case CachePolicy.ValidCacheOnly:
                                return;
                            case CachePolicy.CacheThenRefresh:
                            case CachePolicy.AutoRefresh:
                            case CachePolicy.Forever:
                                // fall through to kick off a cache load.
                                break;
                        }
                    }

                    Debug.WriteLine("{0}: Checking cache for {1} (ID={2})", DateTime.Now, ObjectType.Name, LoadContext.Identity);
                    try
                    {
                        if (_cacheLoader.IsCacheAvailable)
                        {
                            StartLoading(false, _cacheLoader);
                        }
                    }
                    catch
                    {
                        if (isCacheValid)
                        {
                            Debug.WriteLine("{0}: Error cache for {1} (ID={2}), reloading", DateTime.Now, ObjectType.Name, LoadContext.Identity);
                            StartLoading(true, _liveLoader);
                        }
                    }

                }
            }
            finally
            {
                SetBoolValue(LoadPendingMask, false);
            }
        }

        /// <summary>
        /// Start loading operation
        /// </summary>
        /// <param name="force">indicates if loading should be forced</param>
        /// <param name="loader">loader that should be invoked</param>
        private void StartLoading(bool force, ValueLoader loader)
        {
            NextCompletedAction.RegisterActiveLoader(loader.LoaderType);
            try
            {
                loader.FetchData(force);
            }
            catch (Exception)
            {
                NextCompletedAction.UnregisterLoader(loader.LoaderType);
                throw;
            }
        }

        /// <summary>
        /// Notify any listeners of completion of the load, or of any exceptions
        /// that occurred during loading.
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="ex"></param>
        private void NotifyCompletion(ValueLoader loader, Exception ex)
        {
            IUpdatable iupd = ValueInternal as IUpdatable;

            if (iupd != null)
            {
                iupd.IsUpdating = false;
            }

            LoaderType loaderType = loader != null ? loader.LoaderType : LoaderType.CacheLoader;
           
            //  UpdateCompletionHandler makes sure to call handler on UI thread
            //
            try
            {
                if (ex == null)
                {
                    NextCompletedAction.OnSuccess(loaderType);

                    if (_proxyComplitionCallback != null)
                    {
                        _proxyComplitionCallback(this);
                    }
                }
                else
                {
                    NextCompletedAction.OnError(loaderType, ex);
                }

            }
            finally
            {
                // free our value root
                //
                _rootedValue = null;
            }
        }

        /// <summary>
        /// Updates the object value from a source object of the same type.        
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="source"></param>
        private void UpdateFrom(ValueLoader loader, object source)
        {
            Version++;

            // if no one is holding the value, don't bother updating.
            //
            if (!CheckIfAnyoneCares())
            {
                return;
            }

            int version = Version;

            object value = ValueInternal;

            // sure source matches dest
            //
            if (!value.GetType().IsInstanceOfType(source))
            {
                throw new InvalidOperationException("Types not compatible");
            }

            Action handler = () =>
                {
                    // make sure another update hasn't beat us to the punch.
                    if (Version > version)
                    {
                        return;
                    }

                    try {

                        _stats.OnStartUpdate();

                        ReflectionSerializer.UpdateObject(source, value, true, LastUpdatedTime);                        
                    }
                    finally {
                        _stats.OnCompleteUpdate();
                    }
                    // notify successful completion.
                    NotifyCompletion(loader, null);
                };

            if (SynchronousMode)
            {
                handler();
            }
            else
            {
                PriorityQueue.AddUiWorkItem(
                   handler
                );
            }
        }

        /// <summary>
        /// Update the LastUpdated field of our IUpdateble value, 
        /// on the UI thread (it may be bound to UI)
        /// </summary>
        private void UpdateLastUpdated()
        {
            if (CheckIfAnyoneCares())
            {
                IUpdatable updateable = ValueInternal as IUpdatable;

                if (updateable != null)
                {
                    PriorityQueue.AddUiWorkItem(
                        () =>
                        {
                            updateable.LastUpdated = LastUpdatedTime;
                        }
                   );
                }
            }
        }

        /// <summary>
        /// Sets this item into an expired state in order to force a refresh
        /// on the next fetch.
        /// </summary>
        public void SetForRefresh()
        {
            _valueExpirationTime = DateTime.Now;
            
            if (!_liveLoader.IsValid) {
                _liveLoader.Reset();
            }

            // Set the cache loader as expired, which will
            // essentially stop cache loads for this item going forward.
            //
            _cacheLoader.SetExpired();
                                  
        }

        /// <summary>
        /// Clear the cache for this item.
        /// </summary>
        internal void Clear()
        {
            DataManager.StoreProvider.DeleteAll(UniqueName);
        }

        internal enum DataLoadState
        {
            None,
            Loading,
            Loaded,
            Processing,
            ValueAvailable,
            Failed
        }

        internal class ExceptionEventArgs : EventArgs
        {
            public Exception Exception { get; set; }
        }

        internal class ValueAvailableEventArgs : EventArgs
        {
            public object Value { get; set; }
            public DateTime UpdateTime { get; set; }
        }

        /// <summary>
        /// Base ValueLoader class.  This class knows the basic steps
        /// for loading a value and keeping track of where it's at in the load
        /// lifecycle.
        /// </summary>
        internal abstract class ValueLoader
        {
            /// <summary>
            /// The CacheEntry this ValueLoader is associated with.
            /// </summary>
            public CacheEntry CacheEntry
            {
                get;
                private set;
            }

            public UpdateCompletionHandler NextCompletedAction { get; set; }

            // events.
            public event EventHandler Loading;
            public event EventHandler<ValueAvailableEventArgs> ValueAvailable;
            public event EventHandler<ExceptionEventArgs> LoadFailed;

            public ValueLoader(CacheEntry owningEntry)
            {
                CacheEntry = owningEntry;
            }

            public DataLoadState LoadState
            {
                get;
                protected set;
            }

            /// <summary>
            /// The raw data associated with this loader.
            /// </summary>
            public byte[] Data
            {
                get;
                protected set;
            }

            /// <summary>
            /// The loader is busy if it is in a load or a process
            /// action.
            /// </summary>
            public bool IsBusy
            {
                get
                {
                    switch (LoadState)
                    {
                        case DataLoadState.None:
                        case DataLoadState.Failed:
                        case DataLoadState.ValueAvailable:
                            return false;
                        default:
                            return true;
                    }
                }
            }

            /// <summary>
            /// Is this loader in a valid state?
            /// </summary>
            public abstract bool IsValid
            {
                get;
            }

            /// <summary>
            /// Gets type of the loader
            /// </summary>
            public abstract LoaderType LoaderType { get; }

            /// <summary>
            /// Fetch the data for this loader.
            /// </summary>
            /// <param name="force"></param>
            public void FetchData(bool force)
            {
                lock (this)
                {
                    NextCompletedAction = CacheEntry.NextCompletedAction;

                    // make sure we're not already in a loading state.
                    //
                    switch (LoadState)
                    {
                        case DataLoadState.Loading:
                        case DataLoadState.Processing:
                            return;
                        case DataLoadState.Loaded:
                            FireLoading();
                            ProcessData();
                            return;
                    }

                    LoadState = DataLoadState.Loading;

                    try
                    {
                        // kick off the derived class's load
                        if (!FetchDataCore(force))
                        {
                            LoadState = DataLoadState.None;
                        }
                        else
                        {
                            FireLoading();
                        }
                    }
                    catch (Exception ex)
                    {
                        LoadState = DataLoadState.Failed;
                        OnLoadFailed(ex);
                        return;
                    }
                }
            }

            protected abstract bool FetchDataCore(bool force);

            /// <summary>
            /// Fire the Loading event.
            /// </summary>
            private void FireLoading()
            {
                if (Loading != null)
                {
                    Loading(this, EventArgs.Empty);
                }
            }

            /// <summary>
            /// We have data, now deserialize it.
            /// </summary>
            protected void ProcessData()
            {

                if (LoadState == DataLoadState.Processing)
                {
                    return;
                }
                LoadState = DataLoadState.Processing;

                var data = Data;


                try
                {

                    if (data != null)
                    {
                        if (!CacheEntry.CheckIfAnyoneCares())
                        {
                            // no one is listening, so just quit.
                            LoadState = DataLoadState.Loaded;
                            return;
                        }

                        var value = ProcessDataCore(data);

                        // copy the value.
                        //          
                        OnValueAvailable(value, DateTime.MinValue);
                        Data = null;
                    }
                }
                catch (Exception ex)
                {
                    OnLoadFailed(ex);
                    LoadState = DataLoadState.Failed;
                    return;
                }
            }

            protected abstract object ProcessDataCore(byte[] data);

            // Fire the load failed event
            //
            protected void OnLoadFailed(Exception ex)
            {
                LoadState = DataLoadState.Failed;
                if (LoadFailed != null)
                {
                    LoadFailed(this,
                        new ExceptionEventArgs()
                        {
                            Exception = ex
                        }
                    );
                }
            }

            protected virtual void OnValueAvailable(object value, DateTime updateTime)
            {
                LoadState = DataLoadState.ValueAvailable;
               
                if (ValueAvailable != null)
                {
                    ValueAvailable(this, new ValueAvailableEventArgs()
                    {
                        Value = value,
                        UpdateTime = updateTime
                    });
                }
            }

            // Reset the state of this Loader
            public virtual void Reset()
            {
                LoadState = DataLoadState.None;
                Data = null;
            }
        }

        /// <summary>
        /// Loader responsible for managing loads from the cache.
        /// </summary>
        private class CacheValueLoader : ValueLoader
        {
            CacheItemInfo _cacheItemInfo;
            bool _thereIsNoCacheItem;

            public bool IsCacheAvailable
            {
                get
                {
                    return LoadState != DataLoadState.Failed && FindCacheItem();
                }
            }

            public override bool IsValid
            {
                get
                {
                    // Valid if we're not in a failed state and there is a cached value to 
                    // use that is not expired.
                    //
                    if (LoadState != DataLoadState.Failed && FindCacheItem())
                    {
                        return _cacheItemInfo != null && _cacheItemInfo.ExpirationTime > DateTime.Now;
                    }
                    return false;
                }
            }

            /// <summary>
            /// Gets type of the loader
            /// </summary>
            public override LoaderType LoaderType
            {
                get { return LoaderType.CacheLoader; }
            }

            public bool SynchronousMode
            {
                get;
                set;
            }

            public CacheValueLoader(CacheEntry owningEntry)
                : base(owningEntry)
            {

            }

            /// <summary>
            /// Look in the store for a recent entry that we can load from.
            /// </summary>
            /// <returns></returns>
            private bool FindCacheItem()
            {
                lock (this)
                {
                    if (_cacheItemInfo == null && !_thereIsNoCacheItem)
                    {
                        _cacheItemInfo = DataManager.StoreProvider.GetLastestExpiringItem(CacheEntry.UniqueName);
                    }

                    if (_cacheItemInfo == null)
                    {
                        // flat failure.
                        if (!_thereIsNoCacheItem)
                        {
                            _thereIsNoCacheItem = true;
                            Debug.WriteLine("No cache found for {0} (ID={1})", CacheEntry.ObjectType, CacheEntry.LoadContext.Identity);
                        }
                        return false;
                    }
                    return true;
                }
            }

            /// <summary>
            /// Pull the data off the store
            /// </summary>
            protected override bool FetchDataCore(bool force)
            {
                // First check if we have cached data.
                //                
                if (!FindCacheItem())
                {
                    // nope, nevermind.
                    return false;
                }

                // if we have cached data, then mark ourself as loadable and load it up.
                //
                Debug.WriteLine(String.Format("{3}: Loading cached data for {0} (ID={4}), Last Updated={1}, Expiration={2}", CacheEntry.ObjectType.Name, _cacheItemInfo.UpdatedTime, _cacheItemInfo.ExpirationTime, DateTime.Now, CacheEntry.LoadContext.Identity));

                // load it up.
                //
                try
                {
                    Data = DataManager.StoreProvider.Read(_cacheItemInfo);


                    if (Data == null) {
                        OnLoadFailed(new InvalidOperationException("The cache returned no data."));
                        return false;
                    }

                    LoadState = DataLoadState.Loaded;
                    ProcessData();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Cache load failed for {0} (ID={2}): {1}", CacheEntry.ObjectType.Name, ex.ToString(), CacheEntry.LoadContext.Identity);
                    OnLoadFailed(ex);
                    return false;
                }
            }

            protected override void OnValueAvailable(object value, DateTime updateTime)
            {
                // substitute the cache's last updated time.
                //
                base.OnValueAvailable(value, _cacheItemInfo.UpdatedTime);
            }

            protected override object ProcessDataCore(byte[] data)
            {
                // check to see if this is an optimized cache 
                //
                bool isOptimized = _cacheItemInfo.IsOptimized;

                Debug.WriteLine("{0}: Deserializing cached data for {1} (ID={3}), IsOptimized={2}", DateTime.Now, CacheEntry.ObjectType, isOptimized, CacheEntry.LoadContext.Identity);
                using (var stream = new MemoryStream(data)) {

                    try {
                        CacheEntry._stats.OnStartDeserialize();
                        return CacheEntry.DeserializeAction(CacheEntry.LoadContext, stream, isOptimized);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine("{0}: Exception cached data for {1} (ID={2}) Exception=({3})", DateTime.Now, CacheEntry.ObjectType, CacheEntry.LoadContext.Identity, ex);
                        CacheEntry._stats.OnDeserializeFail();
                        throw;
                    }
                    finally {
                        CacheEntry._stats.OnCompleteDeserialize(data.Length);
                    }                    
                }
            }

            public override void Reset()
            {
                base.Reset();
                _cacheItemInfo = null;
                _thereIsNoCacheItem = false;
            }

            /// <summary>
            /// Save the specified value back to the disk.
            /// </summary>
            /// <param name="uniqueName"></param>
            /// <param name="data"></param>
            /// <param name="updatedTime"></param>
            /// <param name="expirationTime"></param>
            /// <param name="isOptimized"></param>
            public void Save(string uniqueName, byte[] data, DateTime updatedTime, DateTime expirationTime, bool isOptimized)
            {
                if (data == null)
                {
                    throw new ArgumentNullException("data");
                }

                _cacheItemInfo = new CacheItemInfo(uniqueName, updatedTime, expirationTime);
                _cacheItemInfo.IsOptimized = isOptimized;
                Data = null;
                LoadState = DataLoadState.None;

                Debug.WriteLine("Writing cache for {0} (ID={3}), IsOptimized={1}, Will expire {2}", CacheEntry.ObjectType.Name, _cacheItemInfo.IsOptimized, _cacheItemInfo.ExpirationTime, CacheEntry.LoadContext.Identity.ToString() );
                DataManager.StoreProvider.Write(_cacheItemInfo, data);
            }

            internal void SetExpired()
            {
                // mark this as a failed load to prevent it from being used in the future.
                //
                LoadState = DataLoadState.Failed;
            }
        }

        /// <summary>
        /// Loader responsible for loading new data.
        /// </summary>
        internal class LiveValueLoader : ValueLoader
        {
            private static TimeSpan RetryTimeout = TimeSpan.FromSeconds(60);

            /// <summary>
            /// If a load fails, we wait 60 seconds before retrying it.  The avoids
            /// reload loops.
            /// </summary>
            private DateTime? _loadRetryTime;

            public override bool IsValid
            {
                get
                {
                    if (LoadState == DataLoadState.Failed && _loadRetryTime.GetValueOrDefault() > DateTime.Now)
                    {
                        return false;
                    }
                    return true;
                }
            }

            /// <summary>
            /// Gets type of the loader
            /// </summary>
            public override LoaderType LoaderType
            {
                get { return LoaderType.LiveLoader; }
            }

            public DateTime UpdateTime
            {
                get;
                private set;
            }

            public LiveValueLoader(CacheEntry entry)
                : base(entry)
            {
            }

            protected override bool FetchDataCore(bool force)
            {
                // I refuse.
                if (!IsValid)
                {
                    return false;
                }

                Debug.WriteLine("{0}: Queuing load for {1} (ID={2})", DateTime.Now, CacheEntry.ObjectType.Name, CacheEntry.LoadContext.Identity);
                CacheEntry._stats.OnStartFetch();
                return CacheEntry.LoadAction(this);
            }

            protected override object ProcessDataCore(byte[] data)
            {
                Debug.WriteLine("{0}: Deserializing live data for {1} (ID={2})", DateTime.Now, CacheEntry.ObjectType, CacheEntry.LoadContext.Identity);
                using (var stream = new MemoryStream(data)) {
                    try {
                        CacheEntry._stats.OnStartDeserialize();
                        return CacheEntry.DeserializeAction(CacheEntry.LoadContext, stream, false);
                    }
                    catch {
                        CacheEntry._stats.OnDeserializeFail();
                        throw;
                    }
                    finally {
                        CacheEntry._stats.OnCompleteDeserialize(data.Length);
                    }
                }
            }

            internal void OnLoadSuccess(Stream result)
            {
                CacheEntry._stats.OnCompleteFetch(true);
                UpdateTime = DateTime.Now;

                // okay, we've got some data.  blow it into 
                // a byte array.
                if (result != null)
                {
                    byte[] bytes = new byte[result.Length];
                    result.Read(bytes, 0, bytes.Length);
                    Data = bytes;
                }

                LoadState = DataLoadState.Loaded;
                base.ProcessData();
            }

            internal void OnLoadFail(LoadRequestFailedException exception)
            {
                CacheEntry._stats.OnCompleteFetch(false);
                // the live load failed, set our retry limit.
                //
                Debug.WriteLine("Live load failed for {0} (ID={2}) Message={1}", exception.ObjectType.Name, exception.Message, exception.LoadContext.Identity);
                _loadRetryTime = DateTime.Now.Add(RetryTimeout);
                LoadState = DataLoadState.Failed;
                OnLoadFailed(exception);
            }

            protected override void OnValueAvailable(object value, DateTime updateTime)
            {
                // note we substitute our caluclated update time for the one passed in.
                //
                base.OnValueAvailable(value, UpdateTime);
            }

            public override void Reset()
            {
                _loadRetryTime = null;
                UpdateTime = DateTime.MinValue;
                base.Reset();
            }


        }

        public void Dispose() {
         
        }
        
    }

}



