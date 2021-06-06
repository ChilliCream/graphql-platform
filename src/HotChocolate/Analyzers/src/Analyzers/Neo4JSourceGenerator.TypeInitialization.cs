using System;
using System.Reflection;
using static System.IO.Path;

namespace HotChocolate.Analyzers
{
    public partial class Neo4JSourceGenerator
    {
        private const string _dll = ".dll";
        private static string _location = 
            GetDirectoryName(typeof(Neo4JSourceGenerator).Assembly.Location)!;

        static Neo4JSourceGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(
            object sender,
            ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name);
                var path = Combine(_location, assemblyName.Name + _dll);
                return Assembly.LoadFrom(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
