using System.Collections.Generic;
using StrawberryShake.Serialization;

namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class EpisodeParser : ILeafValueParser<string, Episode>,  IInputValueFormatter
    {
        public string TypeName => "Episode";

        public Episode Parse(global::System.String serializedValue)
        {
            return serializedValue switch
            {
                "NEW_HOPE" => Episode.NewHope,
                "EMPIRE" => Episode.Empire,
                "JEDI" => Episode.Jedi,
                _ => throw new global::StrawberryShake.GraphQLClientException()
            };
        }

        public object Format(object runtimeValue)
        {
            return runtimeValue switch
            {
                Episode.NewHope => "NEW_HOPE",
                Episode.Empire => "EMPIRE",
                Episode.Jedi => "JEDI",
                _ => throw new global::StrawberryShake.GraphQLClientException()
            };
        }
    }
}
