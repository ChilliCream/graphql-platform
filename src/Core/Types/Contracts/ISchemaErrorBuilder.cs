using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchemaErrorBuilder
    {
        ISchemaErrorBuilder SetMessage(string message);

        ISchemaErrorBuilder SetCode(string code);

        ISchemaErrorBuilder SetPath(IReadOnlyCollection<object> path);

        ISchemaErrorBuilder SetPath(Path path);

        ISchemaErrorBuilder SetTypeSystemObject(ITypeSystemObject typeSystemObject);

        ISchemaErrorBuilder AddSyntaxNode(ISyntaxNode syntaxNode);

        ISchemaErrorBuilder SetExtension(string key, object value);

        ISchemaErrorBuilder SetException(Exception exception);

        ISchemaError Build();
    }

}
