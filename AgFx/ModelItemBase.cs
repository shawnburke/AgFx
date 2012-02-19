// (c) Copyright Microsoft Corporation.
// This source is subject to the Apache License, Version 2.0
// Please see http://www.apache.org/licenses/LICENSE-2.0 for details.
// All other rights reserved.


using System;
using System.Threading;
using System.Windows.Threading;

namespace AgFx
{
    /// <summary>
    /// Generic flavor of ModelItem base.
    /// </summary>
    /// <typeparam name="T">A type of LoadContext</typeparam>
    public class ModelItemBase<T> : ModelItemBase where T : LoadContext {

        /// <summary>
        /// The typed LoadContext property to use with this model.
        /// </summary>
        public new T LoadContext {
            get {
                return (T)base.LoadContext;
            }
            set {
                base.LoadContext = value;
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ModelItemBase() {}

        /// <summary>
        /// Constructor that takes a LoadContext instance.
        /// </summary>
        /// <param name="loadContext">The LoadContext</param>
        public ModelItemBase(T loadContext) : base(){
            LoadContext = loadContext;
        }
    }

    /// <summary>
    /// ModelItem base is the base type to use with most objects processed by DataManager.
    /// </summary>
    public class ModelItemBase : NotifyPropertyChangedBase, IUpdatable
    {

        private LoadContext _lc;

        /// <summary>
        /// The LoadContext for this item.
        /// 
        /// This property is essentially write-once.  It's value can be set once but not modified after
        /// it has been set.
        /// </summary>
        public LoadContext LoadContext {
            get {
                return _lc;
            }
            set {
                if (!Object.Equals(value, _lc)) {

                    if (_lc != null) {
                        throw new InvalidOperationException("Identity can not be changed.");
                    }
                    _lc = value;
                    RaisePropertyChanged("LoadContext");
                }                
            }
        }

        /// <summary>
        /// Default ctor.
        /// </summary>
        public ModelItemBase() : base(true)
        {           
        }

        /// <summary>
        /// Constructor that takes an identity value.  This value will be wrapped into a default LoadContext.
        /// </summary>
        /// <param name="id">The identity of this object, which cannot be null.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ModelItemBase(object id) : this()
        {
            if (id == null) throw new ArgumentNullException();

            LoadContext = new LoadContext(id);
        }
                
        private int _updateCount;

        /// <summary>
        /// Thsi will be set to true when the object is in the process of updating. This property will notify
        /// changes so it can be databound to.
        /// </summary>
        public bool IsUpdating
        {
            get
            {
                return _updateCount > 0;
            }
            set
            {
                bool updating = IsUpdating;

                if (value)
                {
                    _updateCount++;
                }
                else
                {
                    _updateCount = Math.Max(0, --_updateCount);
                    
                }

                if (IsUpdating != updating)
                {  
                    RaisePropertyChanged("IsUpdating");
                }
            }
        }

        private DateTime _lastUpdated;

        /// <summary>
        /// The time that the value for this object was last fetched.
        /// </summary>
        public DateTime LastUpdated
        {
            get {
                return _lastUpdated;
            }
            set
            {
                if (_lastUpdated != value)
                {
                    _lastUpdated = value;
                    RaisePropertyChanged("LastUpdated");
                }
            }
        }

        /// <summary>
        /// Update this object from the passed in source, typically by
        /// copying properties.  Default implementation calls ReflectionSerializer.UpdateObject.
        /// </summary>
        /// <param name="source">The source instance to update from.</param>
        public virtual void UpdateFrom(object source)
        {
            ReflectionSerializer.UpdateObject(source, this, false, null);           
        }

        /// <summary>
        /// Initiate a refresh for this object.  Calls DataManager.Current.Refresh(this)
        /// </summary>
        public void Refresh()
        {
            DataManager.Current.Refresh(this);
        }

        /// <summary>
        /// override
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {            
            ModelItemBase other = obj as ModelItemBase;
            if (other == null) return false;
            return Object.Equals(other.LoadContext, LoadContext);
        }
        
        /// <summary>
        /// override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (LoadContext != null) {
                return LoadContext.GetHashCode();
            }
            return base.GetHashCode();
        }
    }


}
