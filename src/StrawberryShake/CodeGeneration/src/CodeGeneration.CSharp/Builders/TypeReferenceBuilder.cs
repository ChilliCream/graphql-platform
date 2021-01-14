using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class TypeReferenceBuilder : ICodeBuilder
    {
        private string? _name;
        private List<string> _genericTypeArguments = new List<string>();
        private TypeReferenceBuilder? _listInnerType;
        private bool _isNullable = false;
        public static TypeReferenceBuilder New() => new TypeReferenceBuilder();

        public TypeReferenceBuilder SetListType(TypeReferenceBuilder innerType)
        {
            _listInnerType = innerType;
            return this;
        }

        public TypeReferenceBuilder SetName(string name)
        {
            _name = name;
            return this;
        }

        public TypeReferenceBuilder AddGeneric(string name)
        {
            _genericTypeArguments.Add(name);
            return this;
        }

        public TypeReferenceBuilder SetIsNullable(bool isNullable)
        {
            _isNullable = isNullable;
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (_listInnerType is not null)
            {
                await writer.WriteAsync("IReadOnlyList<").ConfigureAwait(false);
                await _listInnerType.BuildAsync(writer);
            }
            else
            {
                await writer.WriteAsync(_name).ConfigureAwait(false);;
            }
            if (_genericTypeArguments.Count > 0)
            {
                await writer.WriteAsync("<").ConfigureAwait(false);;
                for (var i = 0; i < _genericTypeArguments.Count; i++)
                {
                    if (i > 0)
                    {
                        await writer.WriteAsync(", ").ConfigureAwait(false);;
                    }
                    await writer.WriteAsync(_genericTypeArguments[i]).ConfigureAwait(false);;
                }
                await writer.WriteAsync(">").ConfigureAwait(false);;
            }

            if (_isNullable)
            {
                await writer.WriteAsync("?").ConfigureAwait(false);;
            }

            if (_listInnerType is not null)
            {
                await writer.WriteAsync(">").ConfigureAwait(false);;
                if (_listInnerType._isNullable)
                {
                    await writer.WriteAsync("?").ConfigureAwait(false);;
                }
            }
            await writer.WriteSpaceAsync().ConfigureAwait(false);
        }
    }
}
