using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

internal sealed class ExternalSetter(Action<Schema, ObjectType, IValueNode, object> setter)
{
    public void Invoke(Schema schema, ObjectType type, IValueNode data, object obj)
        => setter(schema, type, data, obj);
}
