﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HotChocolate.Subscriptions.Nats {
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class NatsResources {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal NatsResources() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("HotChocolate.Subscriptions.Nats.Properties.NatsResources", typeof(NatsResources).Assembly);
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
        
        internal static string NatsPubSubExtensions_AddNatsSubscriptions_PrefixInvalid {
            get {
                return ResourceManager.GetString("NatsPubSubExtensions_AddNatsSubscriptions_PrefixInvalid", resourceCulture);
            }
        }
        
        internal static string NatsPubSub_NatsPubSub_PrefixCannotBeNull {
            get {
                return ResourceManager.GetString("NatsPubSub_NatsPubSub_PrefixCannotBeNull", resourceCulture);
            }
        }
        
        internal static string Session_Dispose_UnsubscribedFromNats {
            get {
                return ResourceManager.GetString("Session_Dispose_UnsubscribedFromNats", resourceCulture);
            }
        }
        
        internal static string NatsTopic_ConnectAsync_SubscribedToNats {
            get {
                return ResourceManager.GetString("NatsTopic_ConnectAsync_SubscribedToNats", resourceCulture);
            }
        }
    }
}
