namespace Microsoft.Extensions.DependencyInjection

open System.Runtime.CompilerServices
open HotChocolate.Execution.Configuration
open HotChocolate.Types.FSharp

[<Extension>]
type FSharpRequestExecutorBuilderExtensions() =
  [<Extension>]
  static member RegisterFSharpTypeConverters(this: IRequestExecutorBuilder) =
    this.AddTypeConverter<OptionTypeConverter>()

