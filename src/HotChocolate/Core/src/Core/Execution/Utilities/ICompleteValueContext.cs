using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal interface ICompleteValueContext
    {
        IReadOnlyDictionary<string, object> LocalContextData { get; }

        ITypeConversion Converter { get; }

        Path Path { get; set; }

        object Value { get; set; }

        bool HasErrors { get; }

        bool IsViolatingNonNullType { get; set; }

        Action SetElementNull { get; set; }

        ObjectType ResolveObjectType(NameString typeName);

        ObjectType ResolveObjectType(IType type, object resolverResult);

        void AddError(Action<IErrorBuilder> error);

        void AddError(IError error);

        void EnqueueForProcessing(
            ObjectType objectType,
            OrderedDictionary serializedResult,
            object resolverResult);
    }
}
