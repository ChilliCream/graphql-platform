using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal sealed class Directive
        : IDirective
    {
        private readonly object _customDirective;
        private readonly DirectiveNode _parsedDirective;

        public Directive(
            DirectiveType directiveType,
            DirectiveNode parsedDirective)
        {
            Type = directiveType
                ?? throw new ArgumentNullException(nameof(directiveType));
            _parsedDirective = parsedDirective
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
        }

        public string Name { get; }

        public DirectiveType Type { get; }

        public DirectiveResolver Resolver => throw new NotImplementedException();

        public bool IsExecutable => throw new NotImplementedException();

        public T ToObject<T>()
        {
            throw new NotImplementedException();
        }

        public DirectiveNode ToNode()
        {
            throw new NotImplementedException();
        }

        public T GetArgument<T>(string argumentName)
        {
            throw new NotImplementedException();
        }

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
