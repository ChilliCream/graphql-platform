using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class MethodBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier = AccessModifier.Private;
        private Inheritance _inheritance = Inheritance.None;
        private bool _isStatic = false;
        private bool _isAsync = false;
        private string _returnType = "void";
        private string? _name;
        private readonly List<ParameterBuilder> _parameters = new List<ParameterBuilder>();
        private readonly List<ICode> _lines = new List<ICode>();

        public static MethodBuilder New() => new MethodBuilder();

        public MethodBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public MethodBuilder SetStatic()
        {
            _isStatic = true;
            return this;
        }

        public MethodBuilder SetAsync()
        {
            _isAsync = true;
            return this;
        }

        public MethodBuilder SetInheritance(Inheritance value)
        {
            _inheritance = value;
            return this;
        }

        public MethodBuilder SetReturnType(string value, bool condition = true)
        {
            if (condition)
            {
                _returnType = value;
            }
            return this;
        }

        public MethodBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public MethodBuilder AddParameter(ParameterBuilder value)
        {
            _parameters.Add(value);
            return this;
        }

        public MethodBuilder AddCode(ICode value)
        {
            _lines.Add(value);
            return this;
        }

        public MethodBuilder AddCode(string value)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(value));
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);

            await writer.WriteAsync($"{modifier} ").ConfigureAwait(false);

            if (_isStatic)
            {
                await writer.WriteAsync("static ").ConfigureAwait(false);
            }

            if (_isAsync)
            {
                await writer.WriteAsync("async ").ConfigureAwait(false);
            }

            await writer.WriteAsync(
                $"{CreateInheritance()}{_returnType} {_name}(")
                .ConfigureAwait(false);

            if (_parameters.Count == 0)
            {
                await writer.WriteAsync(")").ConfigureAwait(false);
            }
            else if (_parameters.Count == 1)
            {
                await _parameters[0].BuildAsync(writer).ConfigureAwait(false);
                await writer.WriteAsync(")").ConfigureAwait(false);
            }
            else
            {
                await writer.WriteLineAsync().ConfigureAwait(false);

                using (writer.IncreaseIndent())
                {
                    for (int i = 0; i < _parameters.Count; i++)
                    {
                        await writer.WriteIndentAsync().ConfigureAwait(false);
                        await _parameters[i].BuildAsync(writer).ConfigureAwait(false);
                        if (i == _parameters.Count - 1)
                        {
                            await writer.WriteAsync(")").ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteAsync(",").ConfigureAwait(false);
                            await writer.WriteLineAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.WriteIndentedLineAsync("{").ConfigureAwait(false);

            using (writer.IncreaseIndent())
            {
                foreach (ICode code in _lines)
                {
                    await code.BuildAsync(writer).ConfigureAwait(false);
                }
            }

            await writer.WriteIndentedLineAsync("}").ConfigureAwait(false);
        }

        private string CreateInheritance()
        {
            switch (_inheritance)
            {
                case Inheritance.Override:
                    return "override ";

                case Inheritance.Sealed:
                    return "sealed override ";

                case Inheritance.Virtual:
                    return "virtual ";

                default:
                    return string.Empty;
            }
        }
    }
}
