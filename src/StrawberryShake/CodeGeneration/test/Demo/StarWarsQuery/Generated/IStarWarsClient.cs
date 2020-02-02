using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Client.StarWarsQuery
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial interface IStarWarsClient
    {
        Task<IOperationResult<global::StrawberryShake.Client.StarWarsQuery.IGetHuman>> GetHumanAsync(
            Optional<string> id = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Client.StarWarsQuery.IGetHuman>> GetHumanAsync(
            GetHumanOperation operation,
            CancellationToken cancellationToken = default);
    }
}
