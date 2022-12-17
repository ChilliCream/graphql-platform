using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using static System.IO.Path;

namespace HotChocolate.Analyzers;

public partial class Neo4JSourceGenerator
{
    private const string _dll = ".dll";

    private static string _location =
        GetFullPath(GetDirectoryName(typeof(Neo4JSourceGenerator).Assembly.Location)!);

    private static readonly ShadowCopyAnalyzerAssemblyLoader _loader;

    static Neo4JSourceGenerator()
    {
        _loader = new ShadowCopyAnalyzerAssemblyLoader();

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
    }

    private static Assembly? CurrentDomainOnAssemblyResolve(
        object sender,
        ResolveEventArgs args)
    {
        var path = default(string);
        try
        {
            var assemblyName = new AssemblyName(args.Name);
            path = Combine(_location, assemblyName.Name + _dll);

            if (!File.Exists(path))
            {
                return null;
            }

            Debug.WriteLine(path);
            var shadowCopyPath = _loader.GetPathToLoad(path);
            if (!File.Exists(shadowCopyPath))
            {
                return null;
            }

            return Assembly.LoadFrom(shadowCopyPath);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($@"Failure for {path}: {ex}");
            throw;
        }
    }
}
