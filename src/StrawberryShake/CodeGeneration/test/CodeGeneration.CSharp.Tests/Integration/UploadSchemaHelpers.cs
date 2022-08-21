using System.Text;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar;

public static class UploadSchemaHelpers
{

    public static MemoryStream CreateStream(string str)
    {
        var data = new MemoryStream();
        data.Write(Encoding.UTF8.GetBytes(str));
        data.Position = 0;
        return data;
    }

    public static void Configure(IRequestExecutorBuilder builder)
    {
        builder.AddTypeExtension<UploadQueries>().AddType<UploadType>();
    }

    public static string ReadContents(this IFile file)
    {
        using var stream = file.OpenReadStream();
        using var read = new StreamReader(stream);
        return file.Name + ":" + read.ReadToEnd();
    }

    [ExtendObjectType("Query")]
    public class UploadQueries
    {
        public string Upload(
            IFile? single,
            IFile?[]? list,
            IFile?[]?[]? nested,
            [GraphQLName("object")] Test? objectSingle,
            Test?[]? objectList,
            Test?[]?[]? objectNested)
        {
            if (single is not null)
            {
                return single?.ReadContents() ?? "null";
            }

            if (list is not null)
            {
                return string.Join(",", list.Select(x => x?.ReadContents() ?? "null"));
            }

            if (nested is not null)
            {
                return string.Join(",",
                    nested.SelectMany(y => y.Select(x => x?.ReadContents() ?? "null")));
            }

            if (objectSingle is not null)
            {
                return objectSingle.bar!.baz!.file!?.ReadContents() ?? "null";
            }

            if (objectList is not null)
            {
                return string.Join(",",
                    objectList.Select(x => x.bar!.baz!.file!?.ReadContents() ?? "null"));
            }

            if (objectNested is not null)
            {
                return string.Join(",",
                    objectNested.SelectMany(y
                        => y.Select(x => x.bar!.baz!.file!?.ReadContents() ?? "null")));
            }

            return "error";
        }
    }

    public record Test(Bar? bar);

    public record Bar(Baz? baz);

    public record Baz(IFile? file);

}
