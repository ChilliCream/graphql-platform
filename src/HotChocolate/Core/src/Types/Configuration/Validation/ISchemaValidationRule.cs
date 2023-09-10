#nullable enable

using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation;

internal interface ISchemaValidationRule
{
    void Validate(
        ReadOnlySpan<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors);
}
