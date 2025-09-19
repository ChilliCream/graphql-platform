using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools;

public interface IActivity : IDisposable
{
    void WriteError(HCError error);

    void WriteErrors(IEnumerable<HCError> error);
}
