using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    public class FooGen
    {
        private readonly Dictionary<IFragment, InterfaceDescriptor> _interfaces =
            new Dictionary<IFragment, InterfaceDescriptor>();
        private readonly HashSet<string> _names = new HashSet<string>();
        private FieldCollector _fieldCollector;
        private readonly ISchema _schema;

        public FooGen()
        {

        }

        public void Generate(
            IType parent,
            HotChocolate.Path path,
            SelectionSetNode selectionSet)
        {
            INamedType namedType = parent.NamedType();
            foreach (ObjectType objectType in
                _schema.GetPossibleTypes(namedType))
            {
                FieldCollectionResult result =
                    _fieldCollector.CollectFields(
                        objectType, selectionSet, path);
            }
        }

        private InterfaceDescriptor CreateInterfaces(
            IFragmentNode fragmentNode,
            HotChocolate.Path path)
        {
            if (_interfaces.TryGetValue(
                fragmentNode.Fragment,
                out InterfaceDescriptor descriptor))
            {
                return descriptor;
            }

            var implements = new List<IInterfaceDescriptor>();

            foreach (IFragmentNode child in fragmentNode.Children)
            {
                implements.Add(CreateInterfaces(child, path));
            }

            var fieldDescriptors = new List<IFieldDescriptor>();

            if (fragmentNode.Fragment.TypeCondition is IComplexOutputType type)
            {
                var fields = new Dictionary<string, FieldSelection>();

                foreach (FieldNode selection in
                    fragmentNode.Fragment.SelectionSet.Selections.OfType<FieldNode>())
                {
                    FieldCollector.ResolveFieldSelection(
                        type,
                        selection,
                        path,
                        fields);
                }

                fieldDescriptors.AddRange(fields.Values.Select(t =>
                    new FieldDescriptor(t.Field, t.Selection, t.Field.Type)));
            }

            string typeName = CreateName(
                Utilities.NameUtils.GetInterfaceName(
                    fragmentNode.Fragment.Name));

            return new InterfaceDescriptor(
                typeName,
                fragmentNode.Fragment.TypeCondition,
                fieldDescriptors,
                implements);
        }

        private string CreateName(string name) => throw new NotImplementedException();
    }
}
