using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using WellKnownContextData = StrawberryShake.CodeGeneration.Analyzers.WellKnownContextData;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class EntityTypeInterceptor : TypeInterceptor
    {
        private readonly List<TypeInfo> _outputTypes = new();
        private readonly IReadOnlyList<SelectionSetNode> _globalEntityPatterns;
        private readonly IReadOnlyDictionary<NameString, SelectionSetNode> _typeEntityPatterns;

        public EntityTypeInterceptor(
            IReadOnlyList<SelectionSetNode> globalEntityPatterns,
            IReadOnlyDictionary<NameString, SelectionSetNode> typeEntityPatterns)
        {
            _globalEntityPatterns = globalEntityPatterns;
            _typeEntityPatterns = typeEntityPatterns;
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is IComplexOutputType outputType)
            {
                if (_typeEntityPatterns.TryGetValue(outputType.Name, out SelectionSetNode? pattern))
                {
                    contextData[WellKnownContextData.Entity] = pattern;
                }
                else
                {
                    _outputTypes.Add(new TypeInfo(outputType, contextData));
                }
            }
        }

        public override void OnAfterCompleteTypes()
        {
            if (_globalEntityPatterns.Count > 0)
            {
                foreach (TypeInfo typeInfo in _outputTypes)
                {
                    if (_globalEntityPatterns.FirstOrDefault(
                        pattern => DoesPatternMatch(typeInfo.Type, pattern)) is { } matchedPattern)
                    {
                        typeInfo.ContextData[WellKnownContextData.Entity] = matchedPattern;
                    }
                }
            }
        }

        private bool DoesPatternMatch(IComplexOutputType outputType, SelectionSetNode pattern)
        {
            // TODO : At the moment we just allow the first level.

            foreach (var selection in pattern.Selections.OfType<FieldNode>())
            {
                if (selection.SelectionSet is null &&
                    outputType.Fields.TryGetField(selection.Name.Value, out IOutputField? field) &&
                    field.Type.NamedType().IsLeafType())
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private readonly struct TypeInfo
        {
            public TypeInfo(IComplexOutputType type, IDictionary<string, object?> contextData)
            {
                Type = type;
                ContextData = contextData;
            }

            public IComplexOutputType Type { get; }

            public IDictionary<string, object?> ContextData { get; }
        }
    }
}
