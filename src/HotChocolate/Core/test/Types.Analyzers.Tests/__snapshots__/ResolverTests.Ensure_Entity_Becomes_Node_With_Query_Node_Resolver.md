# Ensure_Entity_Becomes_Node_With_Query_Node_Resolver

```json
[
  {
    "Id": "CS8785",
    "Title": "Generator failed to generate source.",
    "Severity": "Warning",
    "WarningLevel": 1,
    "Location": ": (0,0)-(0,0)",
    "MessageFormat": "Generator '{0}' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type '{1}' with message '{2}'.\r\n{3}",
    "Message": "Generator 'GraphQLServerGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'IndexOutOfRangeException' with message 'Index was outside the bounds of the array.'.\r\nSystem.IndexOutOfRangeException: Index was outside the bounds of the array.\n   at System.Collections.Immutable.ImmutableArray`1.get_Item(Int32 index)\n   at HotChocolate.Types.Analyzers.FileBuilders.TypeFileBuilderBase.WriteResolverConstructor(IOutputTypeInfo type, ILocalTypeLookup typeLookup) in /Users/michael/local/hc-2/src/HotChocolate/Core/src/Types.Analyzers/FileBuilders/TypeFileBuilderBase.cs:line 235\n   at HotChocolate.Types.Analyzers.Generators.TypesSyntaxGenerator.WriteFile(TypeFileBuilderBase file, IOutputTypeInfo type, ILocalTypeLookup typeLookup) in /Users/michael/local/hc-2/src/HotChocolate/Core/src/Types.Analyzers/Generators/TypesSyntaxGenerator.cs:line 118\n   at HotChocolate.Types.Analyzers.Generators.TypesSyntaxGenerator.WriteTypes(SourceProductionContext context, ImmutableArray`1 syntaxInfos, StringBuilder sb) in /Users/michael/local/hc-2/src/HotChocolate/Core/src/Types.Analyzers/Generators/TypesSyntaxGenerator.cs:line 68\n   at HotChocolate.Types.Analyzers.Generators.TypesSyntaxGenerator.Generate(SourceProductionContext context, String assemblyName, ImmutableArray`1 syntaxInfos) in /Users/michael/local/hc-2/src/HotChocolate/Core/src/Types.Analyzers/Generators/TypesSyntaxGenerator.cs:line 34\n   at HotChocolate.Types.Analyzers.GraphQLServerGenerator.Execute(SourceProductionContext context, String assemblyName, ImmutableArray`1 syntaxInfos) in /Users/michael/local/hc-2/src/HotChocolate/Core/src/Types.Analyzers/GraphQLServerGenerator.cs:line 150\n   at HotChocolate.Types.Analyzers.GraphQLServerGenerator.<>c.<Initialize>b__6_4(SourceProductionContext context, ValueTuple`2 source) in /Users/michael/local/hc-2/src/HotChocolate/Core/src/Types.Analyzers/GraphQLServerGenerator.cs:line 96\n   at Microsoft.CodeAnalysis.UserFunctionExtensions.<>c__DisplayClass3_0`2.<WrapUserAction>b__0(TInput1 input1, TInput2 input2, CancellationToken token)\n-----\n",
    "Category": "Compiler",
    "CustomTags": [
      "AnalyzerException"
    ]
  }
]
```
