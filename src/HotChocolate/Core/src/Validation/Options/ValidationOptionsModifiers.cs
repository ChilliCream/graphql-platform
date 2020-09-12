using System;
using System.Collections.Generic;

namespace HotChocolate.Validation.Options
{
    public class ValidationOptionsModifiers
    {
        public IList<Action<ValidationOptions>> Modifiers { get; } =
            new List<Action<ValidationOptions>>();
    }
}
