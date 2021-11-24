using System;

namespace HotChocolate.Types.Errors;

public interface IPayloadErrorFactory<out TError, in TException>
    where TException : Exception
{
    TError CreateErrorFrom(TException ex);
}
