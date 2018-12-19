using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class DirectiveRegistry 
    	: IDirectiveRegistry
    {
        private readonly Dictionary<string, DirectiveType> _nameLookup =
            new Dictionary<string, DirectiveType>();
        private readonly Dictionary<Type, DirectiveType> _clrTypeLookup =
            new Dictionary<Type, DirectiveType>();

        public void RegisterDirectiveType<T>()
            where T : DirectiveType, new()
        {
            RegisterDirectiveType(new T());
        }

        public void RegisterDirectiveType(Type t)
        {
            RegisterDirectiveType((DirectiveType)Activator.CreateInstance(t));
        }

        public void RegisterDirectiveType<T>(T directive)
            where T : DirectiveType
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            if (_nameLookup.TryGetValue(directive.Name, out DirectiveType d)
                && d.ClrType != null)
            {
                _clrTypeLookup.Remove(d.ClrType);
            }

            _nameLookup[directive.Name] = directive;

            if (directive.ClrType != null)
            {
                _clrTypeLookup[directive.ClrType] = directive;
            }
        }

        public IReadOnlyCollection<DirectiveType> GetDirectiveTypes()
        {
            return _nameLookup.Values;
        }

        public DirectiveType GetDirectiveType(
            DirectiveReference directiveReference)
        {
            if (directiveReference.Name != null
                && _nameLookup.TryGetValue(
                    directiveReference.Name, out DirectiveType d))
            {
                return d;
            }

            if (directiveReference.ClrType != null
                && _clrTypeLookup.TryGetValue(
                    directiveReference.ClrType, out d))
            {
                return d;
            }

            return null;
        }
    }
}
