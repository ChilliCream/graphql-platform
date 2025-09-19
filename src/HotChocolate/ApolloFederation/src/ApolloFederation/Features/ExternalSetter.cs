using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

internal sealed class ExternalSetter(Action<ObjectType, IValueNode, object> setter)
{
    public void Invoke(ObjectType type, IValueNode data, object obj)
        => setter(type, data, obj);
}
