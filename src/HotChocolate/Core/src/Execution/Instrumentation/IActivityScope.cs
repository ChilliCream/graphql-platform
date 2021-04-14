using System;

namespace HotChocolate.Execution.Instrumentation
{
    /// <summary>
    /// An activity scope represents the execution of a certain activity.
    /// The scope is created when the activity starts and disposed when the activity is completed.
    /// </summary>
    public interface IActivityScope : IDisposable { }

}
