using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    internal class MapMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly FieldReference _fieldReference;
        private readonly FieldDelegate _fieldDelegate;

        public MapMiddleware(
            FieldDelegate next,
            FieldReference fieldReference,
            FieldDelegate fieldDelegate)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _fieldReference = fieldReference
                ?? throw new ArgumentNullException(nameof(fieldReference));
            _fieldDelegate = fieldDelegate
                ?? throw new ArgumentNullException(nameof(fieldDelegate));
        }

        public Task InvokeAsync(IMiddlewareContext context)
        {
            return IsField(context.ObjectType.Name, context.Field.Name)
                ? _fieldDelegate(context)
                : _next(context);
        }

        protected bool IsField(NameString typeName, NameString fieldName)
        {
            return _fieldReference.TypeName.Equals(typeName)
                && _fieldReference.FieldName.Equals(fieldName);
        }
    }
}
