﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HotChocolate.Utilities.Properties {
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class UtilityResources {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal UtilityResources() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("HotChocolate.Utilities.Properties.UtilityResources", typeof(UtilityResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static string MiddlewareActivator_NoInvokeMethod {
            get {
                return ResourceManager.GetString("MiddlewareActivator_NoInvokeMethod", resourceCulture);
            }
        }
        
        internal static string MiddlewareActivator_OneConstructor {
            get {
                return ResourceManager.GetString("MiddlewareActivator_OneConstructor", resourceCulture);
            }
        }
        
        internal static string ActivatorHelper_AbstractTypeError {
            get {
                return ResourceManager.GetString("ActivatorHelper_AbstractTypeError", resourceCulture);
            }
        }
        
        internal static string MiddlewareActivator_ParameterNotSupported {
            get {
                return ResourceManager.GetString("MiddlewareActivator_ParameterNotSupported", resourceCulture);
            }
        }
        
        internal static string ServiceFactory_CreateInstanceFailed {
            get {
                return ResourceManager.GetString("ServiceFactory_CreateInstanceFailed", resourceCulture);
            }
        }
        
        internal static string MiddlewareCompiler_ReturnTypeNotSupported {
            get {
                return ResourceManager.GetString("MiddlewareCompiler_ReturnTypeNotSupported", resourceCulture);
            }
        }
        
        internal static string ArrayWriter_Advance_BufferOverflow {
            get {
                return ResourceManager.GetString("ArrayWriter_Advance_BufferOverflow", resourceCulture);
            }
        }
    }
}
