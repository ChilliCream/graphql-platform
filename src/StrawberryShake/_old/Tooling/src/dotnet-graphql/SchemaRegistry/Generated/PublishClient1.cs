using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class PublishClient1
        : IPublishClient
    {
        public PublishClient1(
            global::StrawberryShake.IPublishClientPayload publishClient)
        {
            PublishClient = publishClient;
        }

        public global::StrawberryShake.IPublishClientPayload PublishClient { get; }
    }
}
