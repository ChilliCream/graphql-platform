---
title: Diagnostic Events
---

This library has recently added _DiagnosticSources_ to offer an
_Instrumentation API_ to collect diagnostic events. To get started we need to
add _two_ packages and implement _two_ classes. So lets get started.

# Installation

We need to add the required packages.

For _.Net Core_ we use the dotnet CLI. Which is perhaps the most preferred way
doing this.

```powershell
dotnet add package System.Diagnostics.DiagnosticSource
dotnet add package Microsoft.Extensions.DiagnosticAdapter
```

And for _.Net Framework_ we still use the following line.

```powershell
Install-Package System.Diagnostics.DiagnosticSource
Install-Package Microsoft.Extensions.DiagnosticAdapter
```

# Implement Diagnostic Listener

After we added those two packages we can start listening to Green Donut
diagnostic events by implementing a _DiagnosticListener_.

First we implement the _DiagnosticListener_.

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Demo
{
    public class CustomListener
    {
        // here you see the complete set of diagnostic events we can listen to.
        // here we usually add just those events we want to listen to.
        // for example if we are not interested to get informed about values
        // loaded from the cache then we just remove the complete method.

        [DiagnosticName("GreenDonut.ExecuteBatchRequest")]
        public void EnableExecuteBatchRequest()
        {
            // this event remains empty. it is required to enable the fetch
            // activity. if you remove this event, ExecuteBatchRequest.Start and
            // ExecuteBatchRequest.Stop stop working.
        }

        [DiagnosticName("GreenDonut.ExecuteBatchRequest.Start")]
        public void HandleExecuteBatchRequestStart(
            IReadOnlyList<object> keys)
        {
            // here goes our code to handle activity start.
        }

        [DiagnosticName("GreenDonut.ExecuteBatchRequest.Stop")]
        public void HandleExecuteBatchRequestStop(
            IReadOnlyList<object> keys,
            IReadOnlyList<object> values)
        {
            // here goes our code to handle activity stop.
        }

        [DiagnosticName("GreenDonut.ExecuteSingleRequest")]
        public void EnableExecuteSingleRequest()
        {
            // this event remains empty. it is required to enable the fetch
            // activity. if you remove this event, ExecuteSingleRequest.Start
            // and ExecuteSingleRequest.Stop stop working.
        }

        [DiagnosticName("GreenDonut.ExecuteSingleRequest.Start")]
        public void HandleExecuteSingleRequestStart(
            object key)
        {
            // here goes our code to handle activity start.
        }

        [DiagnosticName("GreenDonut.ExecuteSingleRequest.Stop")]
        public void HandleExecuteSingleRequestStop(
            object key,
            IReadOnlyList<object> values)
        {
            // here goes our code to handle activity stop.
        }

        [DiagnosticName("GreenDonut.BatchError")]
        public void HandleBatchError(
            IReadOnlyList<object> keys,
            Exception exception)
        {
            // here goes our code to handle batch errors which occur during
            // fetch.
        }

        [DiagnosticName("GreenDonut.CachedValue")]
        public void HandleCachedValue(object key, object cacheKey, object value)
        {
            // here goes our code to handle values coming from the cache.
        }

        [DiagnosticName("GreenDonut.Error")]
        public void HandleError(object key, Exception exception)
        {
            // here goes our code to handle result errors which occur during
            // fetch.
        }
    }
}
```

Then we need to implement an _Observer_ to subscribe to the Green Donut
_DiagnosticSource_ which produces the diagnostic events.

```csharp
using System;
using System.Diagnostics;

namespace Demo
{
    public class CustomObserver
        : IObserver<DiagnosticListener>
    {
        private readonly CustomListener _listener;

        public CustomObserver(CustomListener listener)
        {
            _listener = listener ??
                throw new ArgumentNullException(nameof(listener));
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "GreenDonut")
            {
                value.SubscribeWithAdapter(_listener);
            }
        }
    }
}
```

# Subscribe to Events

Last but not least we must subscribe to the Green Donut _DiagnosticSource_.

```csharp
// subscribe
var listener = new CustomListener();
var observer = new CustomObserver(listener);
var subscription = DiagnosticListener.AllListeners.Subscribe(observer);

// unsubscribe
subscription.Dispose();
```

> **Note**
>
> - Remember to dispose your subscription if you would like to stop listening to
>   diagnostic events.
> - Keep in mind that this code is not production ready. It is meant to give you
>   an idea how it works.
