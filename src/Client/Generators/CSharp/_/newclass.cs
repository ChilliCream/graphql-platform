using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators.CSharp
{
    public class ClassBuilder
        : ITypeBuilder
    {
        private AccessModifier _accessModifier;
        private string? _name;
        private List<MethodBuilder> _methods = new List<MethodBuilder>();

        public static ClassBuilder New() => new ClassBuilder();

        public ClassBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public ClassBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public ClassBuilder AddConstructor()
        {
            throw new NotImplementedException();
        }

        public ClassBuilder AddField()
        {
            throw new NotImplementedException();
        }

        public ClassBuilder AddProperty()
        {
            throw new NotImplementedException();
        }

        public ClassBuilder AddMethod(MethodBuilder method)
        {
            _methods.Add(method);
            return this;
        }

        public async Task BuildAsync(CodeWriter writer)
        {

        }
    }

    public enum Inheritance
    {
        None,
        Sealed,
        Override,
        Virtual
    }

    public class MethodBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier = AccessModifier.Private;
        private Inheritance _inheritance = Inheritance.None;
        private string? _name;
        private List<ParameterBuilder> _parameters = new List<ParameterBuilder>();
        private List<ICode> _lines = new List<ICode>();

        public static MethodBuilder New() => new MethodBuilder();

        public MethodBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public MethodBuilder SetInheritance(Inheritance value)
        {
            _inheritance = value;
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
            string modifier = _accessModifier.ToString().ToLowerInvariant();

            await writer.WriteIndentAsync().ConfigureAwait(false);

            await writer.WriteAsync(
                $"{modifier} {CreateInheritance()}{_name}(")
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
                        }
                        await writer.WriteLineAsync().ConfigureAwait(false);
                    }
                }
            }

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


    public class Foo
    {
        public void Bar()
        {
            CodeFileBuilder.New()
                .AddUsing("System.Collections")
                .SetNamespace("Foo")
                .AddType(ClassBuilder.New()
                    .AddConstructor())
                .BuildAsync(null);

        }
    }

    public interface ICode : ICodeBuilder { }

    public interface ITypeBuilder : ICodeBuilder
    {

    }

    public interface ICodeBuilder
    {
        Task BuildAsync(CodeWriter writer);
    }
}
