using System;

namespace HotChocolate.Types
{
    public class FieldBase<T>
        : TypeSystemBase
        , IField
        , IHasDirectives
        where T : IType
    {
        protected FieldBase(
            FieldDescriptionBase description,
            DirectiveLocation location)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (string.IsNullOrEmpty(description.Name))
            {
                throw new ArgumentException(
                    "The name of a field mustn't be null or empty.",
                    nameof(description));
            }

            Name = description.Name;
            Description = description.Description;
            TypeReference = description.TypeReference;

            var directives = new DirectiveCollection(
                this,
                location,
                description.Directives);
            RegisterForInitialization(directives);

            Directives = directives;
        }

        public INamedType DeclaringType { get; private set; }

        public string Name { get; }

        public string Description { get; }

        public T Type { get; private set; }

        public IDirectiveCollection Directives { get; }

        protected TypeReference TypeReference { get; }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            if (!context.IsDirective && context.Type == null)
            {
                throw new InvalidOperationException(
                    "It is not allowed to initialize a field without " +
                    "a type context.");
            }

            if (TypeReference != null)
            {
                string s = TypeReference.ToString();
                context.RegisterType(TypeReference);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            if (!context.IsDirective && context.Type == null)
            {
                throw new InvalidOperationException(
                    "It is not allowed to initialize a field without " +
                    "a type context.");
            }

            DeclaringType = context.Type;
            Type = context.ResolveFieldType<T>(this, TypeReference);
        }
    }
}
