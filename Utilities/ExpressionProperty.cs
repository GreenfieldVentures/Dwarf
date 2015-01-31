using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf.Utilities
{
    /// <summary>
    /// Class that wraps a PropertyInfo and handles getting and setting of property values via expressions instead for speed
    /// </summary>
    public sealed class ExpressionProperty
    {
        #region Variables

        private Func<object, object> getDelegate;
        private Action<object, object> setDelegate;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ExpressionProperty(PropertyInfo property)
        {
            ContainedProperty = property;
            InitializeGet();
            InitializeSet();
        }

        #endregion Constructors

        #region Properties

        #region ContainedProperty

        /// <summary>
        /// Gets the contained Property
        /// </summary>
        public PropertyInfo ContainedProperty { get; private set; }

        #endregion ContainedProperty

        #region Name

        /// <summary>
        /// Gets the name of the property
        /// </summary>
        public string Name { get { return ContainedProperty.Name; } }

        #endregion Name

        #region PropertyType

        /// <summary>
        /// Gets the type of the property
        /// </summary>
        public Type PropertyType { get { return ContainedProperty.PropertyType; } }

        #endregion PropertyType

        #region CanWrite

        /// <summary>
        /// Gets a value indicating whether the property can be written to
        /// </summary>
        public bool CanWrite
        {
            get { return setDelegate != null; }
        }

        #endregion CanWrite

        #endregion Properties

        #region Methods

        #region InitializeSet

        private void InitializeSet()
        {
            var setMethod = ContainedProperty.GetSetMethod();

            if (setMethod == null)
                return;

            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            var instanceCast = ContainedProperty.DeclaringType.IsValueType ? Expression.Convert(instance, ContainedProperty.DeclaringType) : Expression.TypeAs(instance, ContainedProperty.DeclaringType);
            var valueCast = PropertyType.IsValueType ? Expression.Convert(value, PropertyType) : Expression.TypeAs(value, PropertyType);
            setDelegate = Expression.Lambda<Action<object, object>>(Expression.Call(instanceCast, setMethod, valueCast), new[] { instance, value }).Compile();
        }

        #endregion InitializeSet

        #region InitializeGet

        private void InitializeGet()
        {
            var getMethod = ContainedProperty.GetGetMethod();

            if (getMethod == null)
                return;

            var instance = Expression.Parameter(typeof(object), "instance");
            var instanceCast = ContainedProperty.DeclaringType.IsValueType ? Expression.Convert(instance, ContainedProperty.DeclaringType) : Expression.TypeAs(instance, ContainedProperty.DeclaringType);
            getDelegate = Expression.Lambda<Func<object, object>>(Expression.TypeAs(Expression.Call(instanceCast, getMethod), typeof(object)), instance).Compile();
        }

        #endregion InitializeGet

        #region GetValue

        /// <summary>
        /// Returns the value of the property
        /// </summary>
        public object GetValue(object instance)
        {
            return getDelegate(instance);
        }

        #endregion GetValue

        #region SetValue

        /// <summary>
        /// Sets the property value of the given object
        /// </summary>
        public void SetValue(object instance, object value)
        {
            if (value == null && !PropertyType.IsNullable())
                return;

            setDelegate(instance, value);
        }

        #endregion SetValue

        #region GetCustomAttributes

        /// <summary>
        /// Returns an array containing all the custom attributes
        /// </summary>
        public object[] GetCustomAttributes(bool inherit)
        {
            return ContainedProperty.GetCustomAttributes(inherit);
        }

        #endregion GetCustomAttributes

        #endregion Methods
    }
}
