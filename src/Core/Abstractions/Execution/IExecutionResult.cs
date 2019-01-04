﻿using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IExecutionResult
    {
        IReadOnlyCollection<IError> Errors { get; }
    }
}
