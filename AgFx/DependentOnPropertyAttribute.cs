using System;

namespace AgFx {

    /// <summary>
    /// Attribute for marking one property as dependent on another's value for objects
    /// that derive from NotifyPropertyChangedBase.
    /// 
    /// Properties with this value will have a change notification raised when the property specified by PrimaryPropertyName
    /// raises a change notification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple= true)]
    public class DependentOnPropertyAttribute : Attribute {

        /// <summary>
        /// The primary property that, when changed, will trigger a change notifiation
        /// for the property with this attribute applied.
        /// </summary>
        public string PrimaryPropertyName {
            get;
            set;
        }

        /// <summary>
        /// Disables verification that PrimaryPropertyName is an existing property on the target object.
        /// </summary>
        public bool IsNotARealPropertyName {
            get;
            set;
        }

        /// <summary>
        /// Default ctor.
        /// </summary>
        public DependentOnPropertyAttribute() {

        }

        /// <summary>
        /// Ctor with a primary property name.
        /// </summary>
        /// <param name="primaryPropertyName"></param>
        public DependentOnPropertyAttribute(string primaryPropertyName) {
            PrimaryPropertyName = primaryPropertyName;
        }
    }
}
