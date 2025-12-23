using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Configuration;

internal static class IntrospectionTypeReferences
{
    internal static void Enqueue(
        PriorityQueue<TypeReference, (TypeReferenceStrength, int)> backlog,
        IDescriptorContext context,
        ref int nextIndex)
    {
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__Directive)), nextIndex++);
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__DirectiveLocation)), nextIndex++);
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__EnumValue)), nextIndex++);
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__Field)), nextIndex++);
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__InputValue)), nextIndex++);
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__Schema)), nextIndex++);
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__Type)), nextIndex++);
        EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__TypeKind)), nextIndex++);

        if (context.Options.EnableDirectiveIntrospection)
        {
            EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__AppliedDirective)), nextIndex++);
            EnqueueTypeRef(backlog, context.TypeInspector.GetTypeRef(typeof(__DirectiveArgument)), nextIndex++);
        }

        static void EnqueueTypeRef(
            PriorityQueue<TypeReference, (TypeReferenceStrength, int)> backlog,
            TypeReference typeRef,
            int index)
            => backlog.Enqueue(typeRef, (TypeReferenceStrength.System, index));
    }
}
