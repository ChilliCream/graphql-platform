using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Client.Core.Syntax
{
    public class SyntaxTree
    {
        public OperationDefinition Root { get; private set; }

        public ISelectionSet Head { get; private set; }
        public IList<SelectionSet> SelectionStack { get; private set; }

        public OperationDefinition AddRoot(OperationType type, string name)
        {
            Root = new OperationDefinition(type, name);
            Head = Root;
            SelectionStack = new List<SelectionSet>();
            return Root;
        }

        public FieldSelection AddField(string member, string alias = null)
        {
            return AddField(Head, new FieldSelection(member, alias));
        }

        public FieldSelection AddField(MemberInfo member, MemberInfo alias = null)
        {
            return AddField(Head, member, alias);
        }

        public FieldSelection AddField(ISelectionSet parent, MemberInfo member, MemberInfo alias = null)
        {
            return AddField(parent, new FieldSelection(member, alias));
        }

        public Argument AddArgument(string name, object value)
        {
            var result = new Argument(name, value);
            ((FieldSelection)Head).Arguments.Add(result);
            return result;
        }

        public FragmentDefinition AddFragment(IFragment value)
        {
            var fragmentDefinition = new FragmentDefinition(value.InputType, value.Name);
            Root.FragmentDefinitions.Add(value.Name, fragmentDefinition);
            return fragmentDefinition;
        }

        public InlineFragment AddInlineFragment(Type typeCondition, bool selectTypeName)
        {
            var result = new InlineFragment(typeCondition);

            if (selectTypeName)
            {
                AddField(Head, new FieldSelection("__typename", null), false);
            }

            Head.Selections.Add(result);
            SelectionStack.Add(result);
            Head = result;
            return result;
        }

        public FragmentSpread AddFragmentSpread(string name)
        {
            var fragmentSpread = new FragmentSpread(name);
            Head.Selections.Add(fragmentSpread);
            return fragmentSpread;
        }

        public VariableDefinition AddVariableDefinition(Type type, bool isNullable, string name)
        {
            var result = Root.VariableDefinitions.SingleOrDefault(x => x.Name == name);

            if (result != null && result.Type != VariableDefinition.ToTypeName(type, isNullable))
            {
                throw new InvalidOperationException(
                    $"A variable called '{name}' has already been added with a different type.");
            }

            result = result ?? new VariableDefinition(type, isNullable, name);
            Root.VariableDefinitions.Add(result);
            return result;
        }

        public IDisposable Bookmark()
        {
            var oldHead = Head;
            var oldStack = SelectionStack.ToList();

            return Disposable.Create(() =>
            {
                Head = oldHead;
                SelectionStack = oldStack;
            });
        }

        private FieldSelection AddField(ISelectionSet parent, FieldSelection field, bool updateHead = true)
        {
            var existing = field.Alias == null ?
                parent.Selections
                    .OfType<FieldSelection>()
                    .FirstOrDefault(x => x.Name == field.Name && x.Alias == null) :
                null;

            if (existing == null)
            {
                parent.Selections.Add(field);
            }
            else
            {
                field = existing;
            }

            if (updateHead)
            {
                Head = field;
                SelectionStack.Add(field);
            }

            return field;
        }
    }
}
