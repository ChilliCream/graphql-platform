using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Configuration.Validation;

/// <summary>
/// Implements directive type validation defined in the spec.
/// https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation
/// </summary>
internal sealed class DirectiveValidationRule : ISchemaValidationRule
{
    private const char PrefixCharacter = '_';

    public void Validate(
        IDescriptorContext context,
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        if (context.Options.StrictValidation)
        {
            foreach (var directiveDefinition in schema.DirectiveDefinitions)
            {
                EnsureDirectiveNameIsValid(directiveDefinition, errors);
                EnsureArgumentNamesAreValid(directiveDefinition, errors);
                EnsureArgumentDeprecationIsValid(directiveDefinition, errors);
                EnsureDefaultValuesAreValid(directiveDefinition, errors);
                EnsureNoSelfApplication(directiveDefinition, errors);
            }

            EnsureNoIndirectSelfReference(schema, errors);
        }
    }

    private static void EnsureDirectiveNameIsValid(
        IDirectiveDefinition type,
        ICollection<ISchemaError> errors)
    {
        if (type.Name.Length > 2)
        {
            var firstTwoLetters = type.Name.AsSpan()[..2];

            if (firstTwoLetters[0] == PrefixCharacter
                && firstTwoLetters[1] == PrefixCharacter)
            {
                errors.Add(TwoUnderscoresNotAllowedOnDirectiveName(type));
            }
        }
    }

    private static void EnsureNoSelfApplication(
        IDirectiveDefinition directiveDefinition,
        ICollection<ISchemaError> errors)
    {
        foreach (var directive in directiveDefinition.Directives)
        {
            if (directive.Definition.Name.EqualsOrdinal(directiveDefinition.Name))
            {
                errors.Add(DirectiveDefinitionSelfApplication(directiveDefinition));
                return;
            }
        }

        foreach (var argument in directiveDefinition.Arguments)
        {
            foreach (var directive in argument.Directives)
            {
                if (directive.Definition.Name.EqualsOrdinal(directiveDefinition.Name))
                {
                    errors.Add(DirectiveDefinitionSelfApplication(directiveDefinition));
                    return;
                }
            }
        }
    }

    private static void EnsureNoIndirectSelfReference(
        ISchemaDefinition schema,
        ICollection<ISchemaError> errors)
    {
        var context = new CycleValidationContext(errors);

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            context.VisitedMembers.Clear();
            VisitMember(directiveDefinition, directiveDefinition, context);
        }
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
        // A path of length one is a direct self-reference, which EnsureNoSelfApplication reports.
        if (context.ReferencePath.Count == 1)
        {
            return;
        }

        if (!context.ReportedCycles.Add([.. context.MemberPath]))
        {
            return;
        }

        context.Errors.Add(
            DirectiveDefinitionIndirectSelfReference(
                root,
                context.ReferencePath.Select(source => source.Coordinate)));
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

    private sealed class CycleValidationContext(ICollection<ISchemaError> errors)
    {
        public ICollection<ISchemaError> Errors { get; } = errors;

        public HashSet<ITypeSystemMember> VisitedMembers { get; } = [];

        public List<ITypeSystemMember> MemberPath { get; } = [];

        public List<ISchemaCoordinateProvider> ReferencePath { get; } = [];

        public Dictionary<ITypeSystemMember, List<Reference>> ReferencesByMember { get; } = [];

        public HashSet<HashSet<ITypeSystemMember>> ReportedCycles { get; }
            = new(HashSet<ITypeSystemMember>.CreateSetComparer());
    }
}
