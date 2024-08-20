# GenerateSource_CacheDataLoader_MatchesSnapshot

```json
[
  {
    "Id": "CS8785",
    "Title": "Generator failed to generate source.",
    "Severity": "Warning",
    "WarningLevel": 1,
    "Location": ": (0,0)-(0,0)",
    "Description": "Generator threw the following exception:\r\n'System.InvalidOperationException: Operation is not valid due to the current state of the object.\n   at HotChocolate.Types.Analyzers.FileBuilders.DataLoaderFileBuilder.ExtractMapType(ITypeSymbol returnType) in /Users/michael/local/hc-6/src/HotChocolate/Core/src/Types.Analyzers/FileBuilders/DataLoaderFileBuilder.cs:line 446\n   at HotChocolate.Types.Analyzers.FileBuilders.DataLoaderFileBuilder.WriteDataLoaderLoadMethod(String containingType, IMethodSymbol method, Boolean isScoped, DataLoaderKind kind, ITypeSymbol key, ITypeSymbol value, Dictionary`2 services, Int32 parameterCount, Int32 cancelIndex) in /Users/michael/local/hc-6/src/HotChocolate/Core/src/Types.Analyzers/FileBuilders/DataLoaderFileBuilder.cs:line 342\n   at HotChocolate.Types.Analyzers.Generators.DataLoaderGenerator.GenerateDataLoader(DataLoaderFileBuilder generator, DataLoaderInfo dataLoader, DataLoaderDefaultsInfo defaults, DataLoaderKind kind, ITypeSymbol keyType, ITypeSymbol valueType, Int32 parameterCount, Int32 cancelIndex, Dictionary`2 services) in /Users/michael/local/hc-6/src/HotChocolate/Core/src/Types.Analyzers/Generators/DataLoaderGenerator.cs:line 145\n   at HotChocolate.Types.Analyzers.Generators.DataLoaderGenerator.WriteDataLoader(SourceProductionContext context, ImmutableArray`1 syntaxInfos, DataLoaderDefaultsInfo defaults) in /Users/michael/local/hc-6/src/HotChocolate/Core/src/Types.Analyzers/Generators/DataLoaderGenerator.cs:line 92\n   at HotChocolate.Types.Analyzers.Generators.DataLoaderGenerator.Generate(SourceProductionContext context, Compilation compilation, ImmutableArray`1 syntaxInfos) in /Users/michael/local/hc-6/src/HotChocolate/Core/src/Types.Analyzers/Generators/DataLoaderGenerator.cs:line 18\n   at HotChocolate.Types.Analyzers.GraphQLServerGenerator.Execute(SourceProductionContext context, Compilation compilation, ImmutableArray`1 syntaxInfos) in /Users/michael/local/hc-6/src/HotChocolate/Core/src/Types.Analyzers/GraphQLServerGenerator.cs:line 102\n   at HotChocolate.Types.Analyzers.GraphQLServerGenerator.<>c.<Initialize>b__4_2(SourceProductionContext context, ValueTuple`2 source) in /Users/michael/local/hc-6/src/HotChocolate/Core/src/Types.Analyzers/GraphQLServerGenerator.cs:line 65\n   at Microsoft.CodeAnalysis.UserFunctionExtensions.<>c__DisplayClass3_0`2.<WrapUserAction>b__0(TInput1 input1, TInput2 input2)'.",
    "MessageFormat": "Generator '{0}' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type '{1}' with message '{2}'",
    "Message": "Generator 'GraphQLServerGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'InvalidOperationException' with message 'Operation is not valid due to the current state of the object.'",
    "Category": "Compiler",
    "CustomTags": [
      "AnalyzerException"
    ]
  }
]
```
