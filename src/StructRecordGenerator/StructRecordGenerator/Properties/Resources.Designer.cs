﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace StructRecordGenerators.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("StructRecordGenerators.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///
        ///namespace StructGenerators
        ///{
        ///    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        ///    internal sealed class GenerateStructEqualityAttribute : Attribute
        ///    {
        ///    }
        ///}
        ///.
        /// </summary>
        internal static string GenerateEqualityAttribute {
            get {
                return ResourceManager.GetString("GenerateEqualityAttribute", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///
        ///namespace StructGenerators
        ///{
        ///    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        ///    internal sealed class GenerateToStringAttribute : Attribute
        ///    {
        ///        /// &lt;summary&gt;
        ///        /// If true, the type name will be printed as part of ToString result.
        ///        /// &lt;/summary&gt;
        ///        public bool PrintTypeName { get; set; } = false;
        ///
        ///        /// &lt;summary&gt;
        ///        /// The max length of a final string representation.
        ///       [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string GenerateToStringAttributeFile {
            get {
                return ResourceManager.GetString("GenerateToStringAttributeFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System.Collections;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using static StructGenerators.CollectionsBehavior;
        /////#nullable disable
        ///namespace StructGenerators
        ///{
        ///    internal static class ToStringGenerationHelper
        ///    {
        ///        /// &lt;summary&gt;
        ///        /// Adds a string representation of a given &lt;paramref name=&quot;source&quot;/&gt; into &lt;paramref name=&quot;sb&quot;/&gt; based on &lt;paramref name=&quot;behavior&quot;/&gt;.
        ///        /// &lt;/summary&gt;
        ///        public static void PrintCollection&lt;T&gt;(this Strin [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ToStringGenerationHelper {
            get {
                return ResourceManager.GetString("ToStringGenerationHelper", resourceCulture);
            }
        }
    }
}
