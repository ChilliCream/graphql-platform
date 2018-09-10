using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{

    public class DirectiveType
        : TypeSystemBase
    {
        internal DirectiveType(Action<IDirectiveDescriptor> configure)
        {
            Initialize(configure);
        }

        protected DirectiveType()
        {
            Initialize(Configure);
        }

        public DirectiveDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyCollection<DirectiveLocation> Locations { get; private set; }

        public FieldCollection<InputField> Arguments { get; private set; }

        #region Configuration

        internal virtual DirectiveDescriptor CreateDescriptor() =>
            new DirectiveDescriptor();

        protected virtual void Configure(IDirectiveDescriptor descriptor) { }

        #endregion

        #region  Initialization

        private void Initialize(Action<IDirectiveDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            DirectiveDescriptor descriptor = CreateDescriptor();
            configure(descriptor);

            DirectiveDescription description = descriptor.CreateDescription();

            SyntaxNode = description.SyntaxNode;
            Name = description.Name;
            Description = description.Description;
            Locations = description.Locations.ToImmutableList();
            Arguments = new FieldCollection<InputField>(
                description.Arguments.Select(t => new InputField(t)));
        }

        protected override void OnRegisterDependencies(ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (INeedsInitialization argument in Arguments
                .Cast<INeedsInitialization>())
            {
                argument.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            foreach (INeedsInitialization argument in Arguments
                .Cast<INeedsInitialization>())
            {
                argument.CompleteType(context);
            }
        }

        #endregion
    }

    public class DirectiveType<TDirective>
        : TypeSystemBase
    {

    }

    public interface IDirectiveCollection<out T>
        : IEnumerable<T>
        where T : IDirective
    {
        T this[string fieldName] { get; }

        bool ContainsDirective(string directiveName);
    }
}
