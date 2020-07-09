using System;
using System.Diagnostics;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public sealed class SyntaxTypeReference
        : TypeReference
        , IEquatable<SyntaxTypeReference>
    {
        public SyntaxTypeReference(
            ITypeNode type,
            TypeContext context,
            string? scope = null,
            bool[]? nullable = null)
            : base(context, scope, nullable)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public ITypeNode Type { get; }

        public SyntaxTypeReference Rewrite()
        {
            if (Nullable is null)
            {
                return this;
            }

            Span<bool> segments = stackalloc bool[8];
            var nullable = true;
            ITypeNode? current = Type;

            var i = 0;
            var l = 0;
            while (current is { })
            {
                if (current is NonNullTypeNode)
                {
                    nullable = false;
                }
                else
                {
                    if (i < Nullable.Length)
                    {
                        nullable = Nullable[i++];
                    }

                    if (current is ListTypeNode)
                    {
                        segments[l++] = nullable;
                        nullable = true;
                    }
                    else
                    {
                        Debug.Assert(current is NamedTypeNode);

                        ITypeNode rewritten = nullable
                            ? current
                            : new NonNullTypeNode((INullableTypeNode)current);

                        if (l > 0)
                        {
                            for (var j = l - 1; j >= 0; j--)
                            {
                                rewritten = segments[j]
                                    ? (ITypeNode)new ListTypeNode(rewritten)
                                    : new NonNullTypeNode(new ListTypeNode(rewritten));
                            }
                        }

                        return WithType(rewritten);
                    }
                }

                current = current.InnerType();
            }

            throw new InvalidOperationException();
        }


        public bool Equals(SyntaxTypeReference? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!IsEqual(other))
            {
                return false;
            }

            return Type.IsEqualTo(other.Type);
        }

        public override bool Equals(ITypeReference? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is SyntaxTypeReference c)
            {
                return Equals(c);
            }

            return false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is SyntaxTypeReference c)
            {
                return Equals(c);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return base.GetHashCode() ^ Type.GetHashCode() * 397;
            }
        }

        public override string ToString()
        {
            return $"{Context}: {Type}";
        }

        public SyntaxTypeReference WithType(ITypeNode type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new SyntaxTypeReference(
                type,
                Context,
                Scope,
                Nullable);
        }

        public SyntaxTypeReference WithContext(TypeContext context = TypeContext.None)
        {
            return new SyntaxTypeReference(
                Type,
                context,
                Scope,
                Nullable);
        }

        public SyntaxTypeReference WithScope(string? scope = null)
        {
            return new SyntaxTypeReference(
                Type,
                Context,
                scope,
                Nullable);
        }

        public SyntaxTypeReference WithNullable(bool[]? nullable = null)
        {
            return new SyntaxTypeReference(
                Type,
                Context,
                Scope,
                nullable);
        }

        public SyntaxTypeReference With(
            Optional<ITypeNode> type = default,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default)
        {
            if (type.HasValue && type.Value is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new SyntaxTypeReference(
                type.HasValue ? type.Value! : Type,
                context.HasValue ? context.Value : Context,
                scope.HasValue ? scope.Value : Scope,
                nullable.HasValue ? nullable.Value : Nullable);
        }
    }
}
