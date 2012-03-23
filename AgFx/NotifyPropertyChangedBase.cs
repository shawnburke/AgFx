// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Threading;

namespace AgFx
{
    /// <summary>
    /// Base class for INotifyPropertyChanged implementation.  Gives a RaisePropertyChanged method 
    /// for notifying changes as well a facilities for thread switching and change notification cascading.
    /// </summary>
 
  //  [DataContract]   
    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
    {

        private static int? _UiThreadId;

        /// <summary>
        /// A static set of argument instances, one per property name.
        /// </summary>
        private static readonly Dictionary<string, PropertyChangedEventArgs> _argumentInstances = new Dictionary<string, PropertyChangedEventArgs>();
 
        private static Dictionary<Type, Dictionary<string, List<string>>> _typeDependentProperties = new Dictionary<Type,Dictionary<string,List<string>>>();

        private Dictionary<string, List<string>> _dependentProps;

        private int? _contextThreadId;

        private static object PropertyNotifyLock = new object();

        /// <summary>
        /// INotifyPropertyChanged.PropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private SynchronizationContext _context;

        /// <summary>
        /// Helper for determining if the current thread is the thread that notifications shoudl be fired on.
        /// </summary>
        protected bool IsOnNotificationThread
        {
            get
            {
                if (NotificationContext == null)
                {
                    return false;
                }                
                return this._contextThreadId.HasValue && _contextThreadId.Value == Thread.CurrentThread.ManagedThreadId;
            }
        }
                


        /// <summary>
        /// The notification context on which to fire notifications.  This defaults to the UI thread's context.
        /// </summary>
        protected SynchronizationContext NotificationContext
        {
            get
            {
                return _context;
            }
            set
            {
                if (_context != value)
                {
                    _context = value;

                    // try to grab the UI thread
                    //
                    EnsureUiThreadId();
                    
                    // see if we're on the UI thread
                    //
                    if (value != null && value == DispatcherSynchronizationContext.Current) {
                        _contextThreadId = _UiThreadId;
                    }

                    // If we are on the thread that is being set, snap the thread id.
                    //
                    if (value != null && value == SynchronizationContext.Current) {
                        _contextThreadId = Thread.CurrentThread.ManagedThreadId;
                    }
                }
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NotifyPropertyChangedBase() : this(true) {

        }
        
        /// <summary>
        /// Ctor with thread selection
        /// </summary>
        /// <param name="notifyOnUiThread">Pass true to automatically associate with UI thread.  See NotificationContext property to associate with other threads, or pass false to not associate with a thread.</param>
        public NotifyPropertyChangedBase(bool notifyOnUiThread)
        {
            InitializeDependentProperties();

            if (notifyOnUiThread) {
                NotificationContext = DispatcherSynchronizationContext.Current;            
            }
        }

        private void EnsureUiThreadId() {

            if (_UiThreadId == null && SynchronizationContext.Current != null && SynchronizationContext.Current == DispatcherSynchronizationContext.Current) {
                _UiThreadId = Thread.CurrentThread.ManagedThreadId;            
            }
        }
        
        /// <summary>
        /// Walk through the dependent property attributes, and add them to the 
        /// data structure.
        /// </summary>
        private void InitializeDependentProperties() {

            Type t = GetType();

            lock (t) {
                if (!_typeDependentProperties.ContainsKey(t)) {

                    var propertyLookup = new Dictionary<string, List<string>>();

                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (var prop in props) {
                        var attrs = from a in prop.GetCustomAttributes(typeof(DependentOnPropertyAttribute), true)
                                    where a is DependentOnPropertyAttribute
                                    select (DependentOnPropertyAttribute)a;



                        foreach (var dopa in attrs) {

                            if (!String.IsNullOrEmpty(dopa.PrimaryPropertyName)) {

                                if (!dopa.IsNotARealPropertyName && !props.Any(p => p.Name == dopa.PrimaryPropertyName)) {
                                    throw new ArgumentException(String.Format("PrimaryPropertyName {0} not found on type {1}", dopa.PrimaryPropertyName, t.Name));
                                }
                                AddDependentProperty(propertyLookup, dopa.PrimaryPropertyName, prop.Name);
                            }
                        }
                    }
                    _typeDependentProperties[t] = propertyLookup;

                    _dependentProps = propertyLookup;
                }
            }
        }

        private Dictionary<string, List<string>> GetPropertyLookup() {
         
            if (_dependentProps != null) {
                return _dependentProps;
            }
            else if (_typeDependentProperties.ContainsKey(GetType())) {
                return _typeDependentProperties[GetType()];
            }
            return null;
        }


      
        
        private void AddDependentProperty(Dictionary<string, List<string>> propertyLookup, string primaryPropertyName, string dependantPropertyName)
        {
            List<string> dependantProps;

            if (!propertyLookup.TryGetValue(primaryPropertyName, out dependantProps))
            {
                dependantProps = new List<string>();
                propertyLookup[primaryPropertyName] = dependantProps;
            }
            else
            {
                string loop = FindDependentLoop(propertyLookup, dependantPropertyName, primaryPropertyName);

                if (loop != null)
                {
                    throw new ArgumentException(String.Format("Can't add dependant property {0}->{1} because it is already dependent on {2} and will cause recursion.", primaryPropertyName, dependantPropertyName, loop));
                }
            }

            dependantProps.Add(dependantPropertyName);
        }



        /// Just walk through the dependent list looking for the search value.  If you find it,
        /// we have a recursive loop.  That's bad.     
        private string FindDependentLoop(Dictionary<string, List<string>> propertyLookup, string propertyName, string search)
        {

            List<string> dList;

            if (propertyLookup.TryGetValue(propertyName, out dList))
            {

                foreach (string dp in dList)
                {
                    if (dp == search)
                    {

                        // uh oh, found a loop.
                        return propertyName;
                    }

                    string found = FindDependentLoop(propertyLookup, dp, search);

                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;

        }

        private void RemoveDependentProperty(Dictionary<string, List<string>> propertyLookup, string primaryPropertyName, string dependentPropertyName)
        {
            List<string> props;

            if (_dependentProps.TryGetValue(primaryPropertyName, out props))
            {
                props.Remove(dependentPropertyName);
            }
        }

        /// <summary>
        /// Raise a property change for the given property on the notification thread.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            lock (PropertyNotifyLock)
            {
                Action<string> notify = (s) =>
                {
                    var handler = PropertyChanged;
                    if (handler != null)
                    {
                        PropertyChangedEventArgs args;
                        if (!_argumentInstances.TryGetValue(propertyName, out args))
                        {
                            args = new PropertyChangedEventArgs(propertyName);
                            _argumentInstances[propertyName] = args;
                        }

                        handler(this, args);

                        var depenentProps = GetPropertyLookup();
                        if (depenentProps != null) {

                            // recurse on dependents
                            //
                            List<string> dependents;

                            if (depenentProps.TryGetValue(propertyName, out dependents)) {
                                if (handler != null) {
                                    dependents.ForEach(p =>
                                    {
                                        RaisePropertyChanged(p);
                                    }
                                   );
                                }
                            }
                        }
                    }
                };

                InvokeOnContext(() => notify(propertyName));
            }
        }

        private void InvokeOnContext(Action a) {

            if (!IsOnNotificationThread && _context != null)
            {
                _context.Post(
                    (state) =>
                    {
                        // snap the thread ID.
                        if (_contextThreadId == null)
                        {
                            _contextThreadId = Thread.CurrentThread.ManagedThreadId;
                        }
                        PriorityQueue.AddUiWorkItem(a, true);                        
                    },
                    null
                );
            }
            else
            {
                a();
            }
            
        }

    }
}
