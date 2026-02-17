using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Logging.Contracts;
using HotChocolate.Rules;
using HotChocolate.Types;

namespace HotChocolate;

/// <summary>
/// Represents a schema validator that can be used to validate a schema against a set of rules.
/// </summary>
public sealed class SchemaValidator
{
    private readonly HashSet<object> _rules;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidator"/> class with the default
    /// rules.
    /// </summary>
    public SchemaValidator()
    {
        _rules = [];
        AddDefaultRules();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaValidator"/> class with the specified rules.
    /// </summary>
    /// <param name="rules">The rules to use for validation.</param>
    public SchemaValidator(IEnumerable<object> rules)
    {
        _rules = rules.ToHashSet();
    }

    /// <summary>
    /// Adds the default validation rules to the schema validator.
    /// </summary>
    public void AddDefaultRules()
    {
        _rules.Add(new DirectiveDefinitionIncludesLocationRule());
        _rules.Add(new DirectiveIsDefinedRule());
        _rules.Add(new EnumValueIsDefinedRule());
        _rules.Add(new NoInputObjectCycleRule());
        _rules.Add(new NoInputObjectDefaultValueCycleRule());
        _rules.Add(new NonEmptyEnumTypeRule());
        _rules.Add(new NonEmptyInputObjectTypeRule());
        _rules.Add(new NonEmptyInterfaceTypeRule());
        _rules.Add(new NonEmptyObjectTypeRule());
        _rules.Add(new NonEmptyUnionTypeRule());
        _rules.Add(new NoSelfImplementationRule());
        _rules.Add(new TypeIsDefinedRule());
        _rules.Add(new ValidDeprecationRule());
        _rules.Add(new ValidImplementationsRule());
        _rules.Add(new ValidNameRule());
        _rules.Add(new ValidOneOfFieldRule());
    }

    /// <summary>
    /// Runs the schema validation process.
    /// </summary>
    /// <param name="schema">The schema to validate.</param>
    /// <param name="log">The log to which validation issues will be reported.</param>
    /// <returns></returns>
    public bool Validate(ISchemaDefinition schema, IValidationLog log)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var context = new ValidationContext(schema, log);

        PublishEvents(context);

        return !context.Log.HasErrors;
    }

    private void PublishEvents(ValidationContext context)
    {
        var schema = context.Schema;

        PublishEvent(new InputObjectTypesEvent(schema.Types.OfType<IInputObjectTypeDefinition>()), context);

        foreach (var type in schema.Types)
        {
            PublishEvent(new TypeEvent(type), context);
            PublishDirectiveEvents(type, context);

            if (type is INameProvider namedMember)
            {
                PublishEvent(new NamedMemberEvent(namedMember), context);
            }

            switch (type)
            {
                case IComplexTypeDefinition complexType:
                    PublishEvent(new ComplexTypeEvent(complexType), context);

                    switch (complexType)
                    {
                        case IInterfaceTypeDefinition interfaceType:
                            PublishEvent(new InterfaceTypeEvent(interfaceType), context);
                            break;
                        case IObjectTypeDefinition objectType:
                            PublishEvent(new ObjectTypeEvent(objectType), context);
                            break;
                    }

                    foreach (var field in complexType.Fields)
                    {
                        PublishEvent(new FieldEvent(field), context);
                        PublishEvent(new OutputFieldEvent(field), context);
                        PublishEvent(new NamedMemberEvent(field), context);

                        foreach (var argument in field.Arguments)
                        {
                            PublishEvent(new ArgumentEvent(argument), context);
                            PublishEvent(new InputValueEvent(argument), context);
                            PublishEvent(new NamedMemberEvent(argument), context);

                            PublishDirectiveEvents(argument, context);
                        }

                        PublishDirectiveEvents(field, context);
                    }

                    break;

                case IEnumTypeDefinition enumType:
                    PublishEvent(new EnumTypeEvent(enumType), context);

                    foreach (var value in enumType.Values)
                    {
                        PublishEvent(new EnumValueEvent(value), context);
                        PublishEvent(new NamedMemberEvent(value), context);
                    }

                    break;

                case IInputObjectTypeDefinition inputObjectType:
                    PublishEvent(new InputObjectTypeEvent(inputObjectType), context);

                    foreach (var field in inputObjectType.Fields)
                    {
                        PublishEvent(new FieldEvent(field), context);
                        PublishEvent(new InputFieldEvent(field), context);
                        PublishEvent(new InputValueEvent(field), context);
                        PublishEvent(new NamedMemberEvent(field), context);

                        PublishDirectiveEvents(field, context);
                    }

                    break;

                case IUnionTypeDefinition unionType:
                    PublishEvent(new UnionTypeEvent(unionType), context);
                    break;
            }
        }

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            PublishEvent(new DirectiveDefinitionEvent(directiveDefinition), context);
            PublishEvent(new NamedMemberEvent(directiveDefinition), context);

            foreach (var argument in directiveDefinition.Arguments)
            {
                PublishEvent(new ArgumentEvent(argument), context);
                PublishEvent(new InputValueEvent(argument), context);
                PublishEvent(new NamedMemberEvent(argument), context);
            }
        }
    }

    private void PublishDirectiveEvents(
        IDirectivesProvider member,
        ValidationContext context)
    {
        foreach (var directive in member.Directives)
        {
            PublishEvent(new DirectiveEvent(directive, member), context);

            foreach (var argumentAssignment in directive.Arguments)
            {
                if (!directive.Definition.Arguments.TryGetField(
                    argumentAssignment.Name,
                    out var directiveArgument))
                {
                    continue;
                }

                PublishEvent(
                    new DirectiveArgumentAssignmentEvent(
                        argumentAssignment,
                        directiveArgument,
                        directive,
                        member),
                    context);
            }
        }
    }

    private void PublishEvent<TEvent>(TEvent @event, ValidationContext context)
        where TEvent : IValidationEvent
    {
        foreach (var rule in _rules)
        {
            if (rule is IValidationEventHandler<TEvent> handler)
            {
                handler.Handle(@event, context);
            }
        }
    }
}
