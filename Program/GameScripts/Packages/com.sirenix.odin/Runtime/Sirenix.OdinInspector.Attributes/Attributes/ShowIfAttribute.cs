//-----------------------------------------------------------------------
// <copyright file="ShowIfAttribute.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector
{
#pragma warning disable

    using System;

    /// <summary>
    /// <para>ShowIf is used on any property and can hide the property in the inspector.</para>
    /// <para>Use this to hide irrelevant properties based on the current state of the object.</para>
    /// </summary>
    /// <example>
    /// <para>This example shows a component with fields hidden by the state of another field.</para>
    /// <code>
    /// public class MyComponent : MonoBehaviour
    /// {
    ///		public bool ShowProperties;
    ///
    ///		[ShowIf("showProperties")]
    ///		public int MyInt;
    ///
    ///		[ShowIf("showProperties", false)]
    ///		public string MyString;
    ///		
    ///	    public SomeEnum SomeEnumField;
    ///		
    ///		[ShowIf("SomeEnumField", SomeEnum.SomeEnumMember)]
    ///		public string SomeString;
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// <para>This example shows a component with a field that is hidden when the game object is inactive.</para>
    /// <code>
    /// public class MyComponent : MonoBehaviour
    /// {
    ///		[ShowIf("MyVisibleFunction")]
    ///		public int MyHideableField;
    ///
    ///		private bool MyVisibleFunction()
    ///		{
    ///			return this.gameObject.activeInHierarchy;
    ///		}
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="EnableIfAttribute"/>
    /// <seealso cref="DisableIfAttribute"/>
    /// <seealso cref="HideIfAttribute"/>
    [DontApplyToListElements]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ShowIfAttribute : Attribute
    {
        /// <summary>
        /// The name of a bool member field, property or method. Obsolete; use the Condition member instead.
        /// </summary>
        [Obsolete("Use the Condition member instead.",
#if SIRENIX_INTERNAL
            true
#else
            false
#endif
        )]
        public string MemberName { get { return this.Condition; } set { this.Condition = value; } }

        /// <summary>
        /// A resolved string that defines the condition to check the value of, such as a member name or an expression.
        /// </summary>
        public string Condition;

        /// <summary>
        /// The optional condition value.
        /// </summary>
        public object Value;

        /// <summary>
        /// Whether or not to slide the property in and out when the state changes.
        /// </summary>
        public bool Animate;

        /// <summary>
        /// Shows a property in the inspector, based on the value of a resolved string.
        /// </summary>
        /// <param name="condition">A resolved string that defines the condition to check the value of, such as a member name or an expression.</param>
        /// <param name="animate">Whether or not to slide the property in and out when the state changes.</param>
        public ShowIfAttribute(string condition, bool animate = true)
        {
            this.Condition = condition;
            this.Animate = animate;
        }

        /// <summary>
        /// Shows a property in the inspector, if the resolved string evaluates to the specified value.
        /// </summary>
        /// <param name="condition">A resolved string that defines the condition to check the value of, such as a member name or an expression.</param>
        /// <param name="optionalValue">Value to check against.</param>
        /// <param name="animate">Whether or not to slide the property in and out when the state changes.</param>
        public ShowIfAttribute(string condition, object optionalValue, bool animate = true)
        {
            this.Condition = condition;
            this.Value = optionalValue;
            this.Animate = animate;
        }
    }
}