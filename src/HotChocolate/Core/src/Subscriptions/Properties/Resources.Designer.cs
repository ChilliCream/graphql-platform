﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HotChocolate.Subscriptions.Properties {
    using System;


    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {

        private static System.Resources.ResourceManager resourceMan;

        private static System.Globalization.CultureInfo resourceCulture;

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }

        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("HotChocolate.Subscriptions.Properties.Resources", typeof(Resources).Assembly);
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

        internal static string JsonMessageSerializer_Deserialize_MessageIsNull {
            get {
                return ResourceManager.GetString("JsonMessageSerializer_Deserialize_MessageIsNull", resourceCulture);
            }
        }

        internal static string DefaultPubSub_SubscribeAsync_MinimumAllowedBufferSize {
            get {
                return ResourceManager.GetString("DefaultPubSub_SubscribeAsync_MinimumAllowedBufferSize", resourceCulture);
            }
        }

        internal static string MessageEnumerable_UnsubscribedNotAllowed {
            get {
                return ResourceManager.GetString("MessageEnumerable_UnsubscribedNotAllowed", resourceCulture);
            }
        }

        internal static string MessageEnvelope_DefaultMessage_NeedsBody {
            get {
                return ResourceManager.GetString("MessageEnvelope_DefaultMessage_NeedsBody", resourceCulture);
            }
        }

        internal static string InvalidMessageTypeException_Message {
            get {
                return ResourceManager.GetString("InvalidMessageTypeException_Message", resourceCulture);
            }
        }

        internal static string ConvertFullMode_Value_NotSupported {
            get {
                return ResourceManager.GetString("TopicBufferFullModeExtensions_ConvertFullMode_The_specified_topic_buffer_full_mod" +
                        "e_is_not_supported_", resourceCulture);
            }
        }
    }
}
