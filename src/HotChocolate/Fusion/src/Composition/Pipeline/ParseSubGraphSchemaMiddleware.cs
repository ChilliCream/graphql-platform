using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Fusion.Composition.LogEntryHelper;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ParseSubgraphSchemaMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var excludedTags = context.Features.GetExcludedTags();

        foreach (var config in context.Configurations)
        {
            var schema = SchemaParser.Parse(config.Schema);
            schema.Name = config.Name;
            
            var alignTypes = new AlignTypesVisitor(schema);

            foreach (var sourceText in config.Extensions)
            {
                var extension = SchemaParser.Parse(sourceText);
                alignTypes.VisitSchema(extension, default!);
                CreateMissingTypes(context, schema, extension);
                MergeTypes(context, schema, extension);
                MergeDirectives(extension, schema, schema);
            }

            if (IsIncluded(schema, excludedTags))
            {
                context.Subgraphs.Add(schema);
            }

            foreach (var missingType in schema.Types.OfType<MissingType>())
            {
                context.Log.Write(TypeNotDeclared(missingType, schema));
            }
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsIncluded(Schema schema, IReadOnlySet<string> excludedTags)
    {
        if(schema.Directives.Count == 0)
        {
            return true;
        }
        
        foreach (var directive in schema.Directives[WellKnownDirectives.Tag])
        {
            if (directive.Arguments[0] is { Name: WellKnownDirectives.Name, Value: StringValueNode name, } &&
                excludedTags.Contains(name.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static void CreateMissingTypes(
        CompositionContext context,
        Schema schema,
        Schema extension)
    {
        foreach (var type in extension.Types)
        {
            switch (type)
            {
                case EnumType sourceType:
                    TryCreateMissingType(context, sourceType, schema);
                    break;

                case InputObjectType sourceType:
                    TryCreateMissingType(context, sourceType, schema);
                    break;

                case InterfaceType sourceType:
                    TryCreateMissingType(context, sourceType, schema);
                    break;

                case ObjectType sourceType:
                    TryCreateMissingType(context, sourceType, schema);
                    break;

                case ScalarType sourceType:
                    TryCreateMissingType(context, sourceType, schema);
                    break;

                case UnionType sourceType:
                    TryCreateMissingType(context, sourceType, schema);
                    break;
            }
        }

        foreach (var directiveType in extension.DirectiveTypes)
        {
            if (!schema.DirectiveTypes.ContainsName(directiveType.Name))
            {
                schema.DirectiveTypes.Add(
                    new DirectiveType(directiveType.Name)
                    {
                        IsRepeatable = directiveType.IsRepeatable
                    });
            }
        }
    }

    private void AlignTypes(Schema schema)
    {
        
    }

    private static void TryCreateMissingType<T>(
        CompositionContext context,
        T sourceType,
        Schema targetSchema)
        where T : INamedType, INamedTypeSystemMember<T>
    {
        if (targetSchema.Types.TryGetType(sourceType.Name, out var targetType))
        {
            if (targetType.Kind != sourceType.Kind)
            {
                context.Log.Write(
                    MergeTypeKindDoesNotMatch(
                        sourceType,
                        sourceType.Kind,
                        targetType.Kind));
            }
            return;
        }

        targetType = T.Create(sourceType.Name);
        targetSchema.Types.Add(targetType);
    }

    private static void MergeTypes(
        CompositionContext context,
        Schema schema,
        Schema extension)
    {
        foreach (var type in extension.Types)
        {
            switch (type)
            {
                case EnumType sourceType:
                    MergeEnumType(context, sourceType, schema);
                    break;

                case InputObjectType sourceType:
                    MergeInputType(context, sourceType, schema);
                    break;

                case InterfaceType sourceType:
                    MergeComplexType(context, sourceType, schema);
                    break;

                case ObjectType sourceType:
                    MergeComplexType(context, sourceType, schema);
                    break;

                case ScalarType sourceType:
                    MergeScalarType(sourceType, schema);
                    break;

                case UnionType sourceType:
                    MergeUnionType(sourceType, schema);
                    break;
            }
        }
    }

    private static void MergeEnumType(
        CompositionContext context,
        EnumType source,
        Schema targetSchema)
    {
        if (targetSchema.Types.TryGetType<EnumType>(source.Name, out var target))
        {
            MergeDirectives(source, target, targetSchema);

            if (!string.IsNullOrEmpty(source.Description) &&
                string.IsNullOrEmpty(target.Description))
            {
                target.Description = source.Description;
            }

            foreach (var sourceValue in source.Values)
            {
                if (target.Values.TryGetValue(sourceValue.Name, out var targetValue))
                {
                    if (!string.IsNullOrEmpty(sourceValue.Description) &&
                        string.IsNullOrEmpty(targetValue.Description))
                    {
                        targetValue.Description = sourceValue.Description;
                    }

                    if (sourceValue.IsDeprecated &&
                        string.IsNullOrEmpty(targetValue.DeprecationReason))
                    {
                        targetValue.IsDeprecated = sourceValue.IsDeprecated;
                        targetValue.DeprecationReason = sourceValue.DeprecationReason;
                    }
                }
                else
                {
                    targetValue = new EnumValue(sourceValue.Name);
                    targetValue.Description = sourceValue.Description;
                    targetValue.DeprecationReason = sourceValue.DeprecationReason;
                    targetValue.IsDeprecated = sourceValue.IsDeprecated;
                    target.Values.Add(targetValue);
                }

                MergeDirectives(source, target, targetSchema);
            }
        }
    }

    private static void MergeInputType(
        CompositionContext context,
        InputObjectType source,
        Schema targetSchema)
    {
        if (targetSchema.Types.TryGetType<InputObjectType>(source.Name, out var target))
        {
            MergeDirectives(source, target, targetSchema);

            if (!string.IsNullOrEmpty(source.Description) &&
                string.IsNullOrEmpty(target.Description))
            {
                target.Description = source.Description;
            }

            foreach (var sourceField in source.Fields)
            {
                if (target.Fields.TryGetField(sourceField.Name, out var targetField))
                {
                    context.MergeField(source, sourceField, targetField);
                }
                else
                {
                    targetField = context.CreateField(sourceField, targetSchema);
                    target.Fields.Add(targetField);
                }

                MergeDirectives(sourceField, targetField, targetSchema);
            }
        }
    }

    private static void MergeComplexType<T>(
        CompositionContext context,
        T source,
        Schema targetSchema)
        where T : ComplexType
    {
        if (targetSchema.Types.TryGetType<T>(source.Name, out var target))
        {
            MergeDirectives(source, target, targetSchema);

            if (!string.IsNullOrEmpty(source.Description) &&
                string.IsNullOrEmpty(target.Description))
            {
                target.Description = source.Description;
            }

            foreach (var sourceField in source.Fields)
            {
                if (target.Fields.TryGetField(sourceField.Name, out var targetField))
                {
                    context.MergeField(sourceField, targetField, target.Name);
                }
                else
                {
                    targetField = context.CreateField(sourceField, targetSchema);
                    target.Fields.Add(targetField);
                }

                foreach (var sourceArgument in sourceField.Arguments)
                {
                    if (targetField.Arguments.TryGetField(
                        sourceArgument.Name,
                        out var targetArgument))
                    {
                        MergeDirectives(sourceArgument, targetArgument, targetSchema);
                    }
                }

                MergeDirectives(sourceField, targetField, targetSchema);
            }
        }
    }

    private static void MergeScalarType(
        ScalarType source,
        Schema targetSchema)
    {
        if (targetSchema.Types.TryGetType<ScalarType>(source.Name, out var target))
        {
            MergeDirectives(source, target, targetSchema);

            if (!string.IsNullOrEmpty(source.Description) &&
                string.IsNullOrEmpty(target.Description))
            {
                target.Description = source.Description;
            }
        }
    }

    private static void MergeUnionType(
        UnionType source,
        Schema targetSchema)
    {
        if (targetSchema.Types.TryGetType<UnionType>(source.Name, out var target))
        {
            MergeDirectives(source, target, targetSchema);

            if (!string.IsNullOrEmpty(source.Description) &&
                string.IsNullOrEmpty(target.Description))
            {
                target.Description = source.Description;
            }
        }
    }

    private static void MergeDirectives<T>(T source, T target, Schema targetSchema)
        where T : ITypeSystemMember, IHasDirectives
    {
        foreach (var sourceDirective in source.Directives)
        {
            var targetDirectiveType = targetSchema.DirectiveTypes[sourceDirective.Name];
            var targetDirective = target.Directives.FirstOrDefault(sourceDirective.Name);
            var newTargetDirective = new Directive(targetDirectiveType, sourceDirective.Arguments);

            if (targetDirective is null || targetDirectiveType.IsRepeatable)
            {
                target.Directives.Add(newTargetDirective);
            }
            else
            {
                target.Directives.Replace(targetDirective, newTargetDirective);
            }
        }
    }
}