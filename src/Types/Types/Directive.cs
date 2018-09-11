using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal sealed class Directive
        : IDirective
    {
        private object _customDirective;

        public Directive(
            DirectiveType directiveType,
            DirectiveNode parsedDirective)
        {
            Type = directiveType
                ?? throw new ArgumentNullException(nameof(directiveType));
            Node = parsedDirective
                ?? throw new ArgumentNullException(nameof(parsedDirective));
            Name = directiveType.Name;
        }

        public Directive(DirectiveType directiveType, object customDirective)
        {
            Type = directiveType
                ?? throw new ArgumentNullException(nameof(directiveType));
            _customDirective = customDirective
                ?? throw new ArgumentNullException(nameof(customDirective));
            Name = directiveType.Name;
            Node = SerializeCustomDirective(directiveType, customDirective);
        }

        public string Name { get; }

        public DirectiveType Type { get; }

        public DirectiveNode Node { get; }

        public bool IsMiddleware { get; }

        public bool IsResolver { get; }

        public T CreateArguments<T>() => throw new NotImplementedException();

        public T CreateArgument<T>(string argumentName) => throw new NotImplementedException();

        public IDirectiveFieldResolver CreateResolver() => throw new NotImplementedException();

        public IDirectiveFieldResolverHandler CreateMiddleware() => throw new NotImplementedException();

        private static DirectiveNode SerializeCustomDirective(DirectiveType directiveType, object customDirective)
        {
            throw new NotImplementedException();
        }

        internal static Directive FromDescription(DirectiveType directiveType, DirectiveDescription description)
        {
            return null;
        }
    }
}
