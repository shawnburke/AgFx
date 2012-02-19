// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AgFx
{
    /// <summary>
    /// Class responsible for all result hanlders. Allows to have multiply subscribers to the load operation results
    /// </summary>
    internal class UpdateCompletionHandler
    {
        /// <summary>
        /// Lock object to protect a list of subscribers
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// List of subscribers that need be notified about callback
        /// </summary>
        private List<CompletionHandler> _subscribers = new List<CompletionHandler>();

        /// <summary>
        /// List of active loader
        /// </summary>
        private readonly List<LoaderType> _activeLoaders = new List<LoaderType>();

        /// <summary>
        /// Parent cache entry
        /// </summary>
        private readonly CacheEntry _cacheEntry;

        /// <summary>
        /// Set of results produced by loaders
        /// </summary>
        private List<LoadResult> _loadResults = new List<LoadResult>();

        /// <summary>
        /// Initializes a new instance of the UpdateCompletionHandler class 
        /// </summary>
        /// <param name="entry">parent <see cref="CacheEntry"/></param>
        public UpdateCompletionHandler(CacheEntry entry)
        {
            _cacheEntry = entry;
        }

        /// <summary>
        /// Get and Set for unhandled error 
        /// </summary>
        public Action<Exception> UnhandledError { get; set; }

        /// <summary>
        /// Add new subscription. Appropriate callback will be called only once. 
        /// </summary>
        /// <param name="successHandler">success action</param>
        /// <param name="errorHandler">error action</param>
        public void Subscribe<T>(Action<T> successHandler, Action<Exception> errorHandler)
            where T : new()
        {
            Action onSuccessAction = null;

            if (successHandler != null)
            {
                onSuccessAction = () => successHandler((T)_cacheEntry.ValueInternal);
            }

            lock (_lock)
            {
                _subscribers.Add(new CompletionHandler { SuccessAction = onSuccessAction, ErrorAction = errorHandler });
            }
        }

        /// <summary>
        /// Notify all subscribers about success
        /// </summary>
        public void OnSuccess(LoaderType loader)
        {
            lock (_lock)
            {
                _loadResults.Add(new LoadResult {Loader = loader});
                UnregisterLoader(loader);
            }
        }

        /// <summary>
        /// Process results from the loaders
        /// Call MUST be made under a lock
        /// </summary>
        private void ProcessResults()
        {
            List<LoadResult> orderedResults;

            switch (_cacheEntry.CachePolicy)
            {
                case CachePolicy.CacheThenRefresh:
                    {
                        orderedResults = _loadResults.Where(result => result.Loader == LoaderType.CacheLoader).ToList();
                        _loadResults = _loadResults.Where(result => result.Loader != LoaderType.CacheLoader).ToList();

                        if (!_activeLoaders.Contains(LoaderType.CacheLoader))
                        {
                            //There is no cache loader active - we can return live results
                            orderedResults.AddRange(_loadResults);
                            _loadResults = new List<LoadResult>();
                        }
                    }
                    break;
                case CachePolicy.NoCache:
                case CachePolicy.ValidCacheOnly:
                case CachePolicy.AutoRefresh:
                case CachePolicy.Forever:
                    //oder doesn't metter 
                    orderedResults = _loadResults;
                    _loadResults = new List<LoadResult>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (orderedResults != null && orderedResults.Count() > 0)
            {
                bool errorHandlerCalled = false;

                List<CompletionHandler> subscribers = new List<CompletionHandler>(_subscribers);
                foreach (var loadResult in orderedResults)
                {
                    if(loadResult.Error == null)
                    {
                        foreach (var subscriber in subscribers.Where(subscriber => subscriber.SuccessAction != null))
                        {
                            CompletionHandler localSubscriber = subscriber;
                            PriorityQueue.AddUiWorkItem(() => localSubscriber.SuccessAction(), false);
                        }
                    }
                    else
                    {
                        foreach (var subscriber in subscribers.Where(subscriber => subscriber.ErrorAction != null))
                        {
                            var localSubscriber = subscriber;
                            LoadResult localResult = loadResult;
                            PriorityQueue.AddUiWorkItem(() => localSubscriber.ErrorAction(localResult.Error), false);
                            errorHandlerCalled = true;
                        }
                    }

                    // if the error isn't handled, throw.
                    if (loadResult.Error != null && !errorHandlerCalled)
                    {
#if DEBUG
                        var st = _cacheEntry.LastLoadStackTrace;
                        _cacheEntry.LastLoadStackTrace = null;

                        Debug.WriteLine("{4}: FAIL loading {0} (ID={1}).  Exception {2} Message={3}",
                            _cacheEntry.ObjectType.Name, 
                            _cacheEntry.LoadContext.Identity,
                            loadResult.Error.GetType().Name, 
                            loadResult.Error.Message, 
                            DateTime.Now);

                        if (st != null)
                        {
                            Debug.WriteLine("Load initiated from:\r\n" + st);
                        }
#endif
                        if (UnhandledError != null)
                        {
                            UnhandledError(loadResult.Error);
                        }
                    }
                }
                
                if (_activeLoaders.Count == 0)
                {
                    _subscribers = _subscribers.Except(subscribers).ToList();
                }
            }
        }

        /// <summary>
        /// Notify all subscribers about error
        /// </summary>
        /// <param name="loader">type of the loader</param>
        /// <param name="ex">exception</param>
        public void OnError(LoaderType loader, Exception ex)
        {
            lock (_lock)
            {
                _loadResults.Add(new LoadResult { Loader = loader, Error = ex});
                UnregisterLoader(loader);
            }
        }

        /// <summary>
        /// Register pending loader
        /// </summary>
        /// <param name="loader">loader type that is working right now</param>
        public void RegisterActiveLoader(LoaderType loader)
        {
            lock (_lock)
            {
                _activeLoaders.Add(loader);
            }
        }

        /// <summary>
        /// Unregister pending loader
        /// </summary>
        /// <param name="loader">loader type</param>
        public void UnregisterLoader(LoaderType loader)
        {
            lock (_lock)
            {
                _activeLoaders.Remove(loader);
                ProcessResults();
            }
        }

        /// <summary>
        /// Class to hold completion handlers
        /// </summary>
        private class CompletionHandler
        {
            /// <summary>
            /// Action that should be executed in case of success
            /// </summary>
            public Action SuccessAction;
            /// <summary>
            /// Action that should be executed in case of failure
            /// </summary>
            public Action<Exception> ErrorAction;
        }

        /// <summary>
        /// Class holds update results
        /// </summary>
        private class LoadResult
        {
            /// <summary>
            /// Loader type that produced a result
            /// </summary>
            public LoaderType Loader { get; set; }

            /// <summary>
            /// Error produced by the loader
            /// </summary>
            public Exception Error { get; set; }
        }
    }
}