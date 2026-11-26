using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Logging;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// A Directive definition must not contain the use of a Directive which references itself
/// indirectly by referencing a Type or Directive which transitively includes a reference to this
/// Directive.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation">
/// Specification
/// </seealso>
public sealed class DirectiveDefinitionNoIndirectSelfReferenceRule
    : IValidationEventHandler<DirectiveDefinitionsEvent>
{
    /// <summary>
    /// Checks that no directive applied to a directive definition or to one of its arguments leads
    /// back to that definition through other directives or types. Each cycle is reported once.
    /// </summary>
    public void Handle(DirectiveDefinitionsEvent @event, ValidationContext context)
    {
        var cycleValidationContext = new CycleValidationContext();

        foreach (var directiveDefinition in @event.DirectiveDefinitions)
        {
            cycleValidationContext.VisitedMembers.Clear();
            VisitMember(directiveDefinition, directiveDefinition, cycleValidationContext);
        }

        context.Log.Write(cycleValidationContext.LogEntries);
    }

    private static void VisitMember(
        ITypeSystemMember member,
        IDirectiveDefinition root,
        CycleValidationContext context)
    {
        if (!context.VisitedMembers.Add(member))
        {
            return;
        }

        context.MemberPath.Add(member);

        foreach (var reference in GetReferences(member, context))
        {
            var (source, referencedMember, isDirectiveUse) = reference;

            // A cycle starts with a directive applied to the root or to one of its arguments.
            if (!isDirectiveUse && ReferenceEquals(member, root))
            {
                continue;
            }

            context.ReferencePath.Add(source);

            if (ReferenceEquals(referencedMember, root))
            {
                ReportCycle(root, context);
            }
            else
            {
                VisitMember(referencedMember, root, context);
            }

            context.ReferencePath.RemoveAt(context.ReferencePath.Count - 1);
        }

        context.MemberPath.RemoveAt(context.MemberPath.Count - 1);
    }

    private static void ReportCycle(IDirectiveDefinition root, CycleValidationContext context)
    {
        // A path of length one is a direct self-reference, which
        // DirectiveDefinitionNoSelfReferenceRule reports.
        if (context.ReferencePath.Count == 1)
        {
            return;
        }

        if (!context.ReportedCycles.Add([.. context.MemberPath]))
        {
            return;
        }

        context.LogEntries.Add(
            DirectiveDefinitionIndirectSelfReference(
                root,
                context.ReferencePath.Select(source => source.Coordinate.ToString()).ToArray()));
    }

    private static List<Reference> GetReferences(
        ITypeSystemMember member,
        CycleValidationContext context)
    {
        if (context.ReferencesByMember.TryGetValue(member, out var references))
        {
            return references;
        }

        references = [];

        switch (member)
        {
            case IDirectiveDefinition directiveDefinition:
                AddDirectiveReferences(directiveDefinition, references);

                foreach (var argument in directiveDefinition.Arguments)
                {
                    AddInputValueReferences(argument, references);
                }

                break;

            case IEnumTypeDefinition enumType:
                AddDirectiveReferences(enumType, references);

                foreach (var value in enumType.Values)
                {
                    AddDirectiveReferences(value, references);
                }

                break;

            case IInputObjectTypeDefinition inputObjectType:
                AddDirectiveReferences(inputObjectType, references);

                foreach (var field in inputObjectType.Fields)
                {
                    AddInputValueReferences(field, references);
                }

                break;

            case ITypeDefinition typeDefinition:
                AddDirectiveReferences(typeDefinition, references);
                break;
        }

        context.ReferencesByMember[member] = references;

        return references;
    }

    private static void AddDirectiveReferences<TMember>(TMember member, List<Reference> references)
        where TMember : IDirectivesProvider, ISchemaCoordinateProvider
    {
        foreach (var directive in member.Directives)
        {
            references.Add(new Reference(member, directive.Definition, IsDirectiveUse: true));
        }
    }

    private static void AddInputValueReferences(
        IInputValueDefinition inputValue,
        List<Reference> references)
    {
        AddDirectiveReferences(inputValue, references);
        references.Add(
            new Reference(inputValue, inputValue.Type.NamedType(), IsDirectiveUse: false));
    }

    private readonly record struct Reference(
        ISchemaCoordinateProvider Source,
        ITypeSystemMember Member,
        bool IsDirectiveUse);

    private sealed class CycleValidationContext
    {
        public HashSet<ITypeSystemMember> VisitedMembers { get; } = [];

        public List<ITypeSystemMember> MemberPath { get; } = [];

        public List<ISchemaCoordinateProvider> ReferencePath { get; } = [];

        public Dictionary<ITypeSystemMember, List<Reference>> ReferencesByMember { get; } = [];

        public HashSet<HashSet<ITypeSystemMember>> ReportedCycles { get; }
            = new(HashSet<ITypeSystemMember>.CreateSetComparer());

        public List<LogEntry> LogEntries { get; } = [];
    }
}
