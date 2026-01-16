using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal static class BuiltInDirectiveTypeReferences
{
    internal static void Enqueue(
        PriorityQueue<TypeReference, (TypeReferenceStrength, int)> backlog,
        IDescriptorContext context,
        ref int nextIndex)
    {
        var typeInspector = context.TypeInspector;

        if (context.Options.EnableOneOf)
        {
            EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(OneOfDirectiveType)), nextIndex++);
        }

        if (context.Options.EnableDefer)
        {
            EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(DeferDirectiveType)), nextIndex++);
        }

        if (context.Options.EnableStream)
        {
            EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(StreamDirectiveType)), nextIndex++);
        }

        if (context.Options.EnableSemanticNonNull)
        {
            EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(SemanticNonNullDirective)), nextIndex++);
        }

        if (context.Options.EnableTag)
        {
            EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(Tag)), nextIndex++);
        }

        EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(SkipDirectiveType)), nextIndex++);
        EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(IncludeDirectiveType)), nextIndex++);
        EnqueueTypeRef(backlog, typeInspector.GetTypeRef(typeof(DeprecatedDirectiveType)), nextIndex++);

        static void EnqueueTypeRef(
            PriorityQueue<TypeReference, (TypeReferenceStrength, int)> backlog,
            TypeReference typeRef,
            int index)
            => backlog.Enqueue(typeRef, (TypeReferenceStrength.System, index));
    }
}
