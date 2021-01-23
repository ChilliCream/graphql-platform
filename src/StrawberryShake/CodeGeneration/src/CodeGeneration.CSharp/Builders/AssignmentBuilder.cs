using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StrawberryShake.Properties;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class AssignmentBuilder : ICode
    {
        private ICode? _leftHandSide;
        private ICode? _rightHandSide;
        private bool _assertNonNull;
        private string? _nonNullAssertTypeNameOverride;

        public static AssignmentBuilder New() => new AssignmentBuilder();

        public AssignmentBuilder SetLefthandSide(ICode value)
        {
            _leftHandSide = value;
            return this;
        }

        public AssignmentBuilder SetLefthandSide(string value)
        {
            _leftHandSide = new CodeInlineBuilder().SetText(value);
            return this;
        }

        public AssignmentBuilder SetRighthandSide(ICode value)
        {
            _rightHandSide = value;
            return this;
        }

        public AssignmentBuilder SetRighthandSide(string value)
        {
            _rightHandSide = new CodeInlineBuilder().SetText(value);
            return this;
        }

        public AssignmentBuilder AssertNonNull(string? nonNullAssertTypeNameOverride = null)
        {
            _nonNullAssertTypeNameOverride = nonNullAssertTypeNameOverride;
            return SetAssertNonNull(true);
        }

        public AssignmentBuilder SetAssertNonNull(bool value)
        {
            _assertNonNull = value;
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_leftHandSide is null || _rightHandSide is null)
            {
                throw new CodeGeneratorException(Resources.AssignmentBuilder_Build_Incomplete);
            }

            writer.WriteIndent();
            _leftHandSide.Build(writer);
            writer.Write(" = ");
            _rightHandSide.Build(writer);
            if (_assertNonNull)
            {
                writer.WriteLine();
                using (writer.IncreaseIndent())
                {
                    writer.WriteIndent();
                    writer.Write(" ?? ");
                    writer.Write($"throw new {TypeNames.ArgumentNullException}(nameof(");
                    if (_nonNullAssertTypeNameOverride is not null)
                    {
                        writer.Write(_nonNullAssertTypeNameOverride);
                    }
                    else
                    {
                        _rightHandSide.Build(writer);
                    }
                    writer.Write("))");
                }
            }
            writer.Write(";");
            writer.WriteLine();
        }
    }
}
