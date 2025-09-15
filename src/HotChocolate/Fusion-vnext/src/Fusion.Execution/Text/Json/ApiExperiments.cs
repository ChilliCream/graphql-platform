using System.Text.Json;

namespace HotChocolate.Text.Json;

public class ApiExperiments
{
    public void Foo(
        CompositeJsonDocument doc,
        CompositeResultElement element,
        JsonElement source)
    {
        var obj = element.Document.CreateObject(selectionSet);
        element.SetProperty(obj);


        element.TrySetNull()



    }
}
