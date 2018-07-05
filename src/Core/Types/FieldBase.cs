using System;

namespace HotChocolate.Types
{
    public class FieldBase
        : TypeSystemBase
        , IField
    {
        protected FieldBase(string name, string description)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(
                    "The name of a field mustn't be null or empty.",
                    nameof(name));
            }

            Name = name;
            Description = description;
        }

        public INamedType DeclaringType { get; private set; }

        public string Name { get; }

        public string Description { get; }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            if (context.Type == null)
            {
                throw new InvalidOperationException(
                    "It is not allowed to initialize a field without " +
                    "a type context.");
            }
        }

        protected override void OnCompleteType(ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            if (context.Type == null)
            {
                throw new InvalidOperationException(
                    "It is not allowed to initialize a field without " +
                    "a type context.");
            }

            DeclaringType = context.Type;
        }
    }
}
