using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class AssignmentBuilder : ICode
    {
        private ICode _leftHandSide { get; set; }
        private ICode _rightHandSide { get; set; }
        private bool _assertNonNull { get; set; }
        private string? _nonNullAssertTypeNameOverride { get; set; }

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


        public async Task BuildAsync(CodeWriter writer)
        {
            await writer.WriteIndentAsync().ConfigureAwait(false);
            await _leftHandSide.BuildAsync(writer).ConfigureAwait(false);
            await writer.WriteAsync(" = ").ConfigureAwait(false);
            await _rightHandSide.BuildAsync(writer).ConfigureAwait(false);
            if (_assertNonNull)
            {
                await writer.WriteLineAsync().ConfigureAwait(false);
                using (writer.IncreaseIndent())
                {
                    await writer.WriteIndentAsync().ConfigureAwait(false);
                    await writer.WriteAsync(" ?? ").ConfigureAwait(false);
                    await writer.WriteAsync("throw new ArgumentNullException(nameof(").ConfigureAwait(false);
                    if (_nonNullAssertTypeNameOverride is not null)
                    {
                        await writer.WriteAsync(_nonNullAssertTypeNameOverride).ConfigureAwait(false);
                    }
                    else
                    {
                        await _rightHandSide.BuildAsync(writer).ConfigureAwait(false);
                    }
                    await writer.WriteAsync("))").ConfigureAwait(false);
                }
            }
            await writer.WriteAsync(";").ConfigureAwait(false);
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
