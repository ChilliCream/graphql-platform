using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using StrawberryShake.VisualStudio.Messages;
using StreamJsonRpc;

namespace StrawberryShake.VisualStudio
{
    public partial class GraphQLLanguageClient
    {       
        public class MessageHandler
        {
            public string RootDirectory { get; set; }

            [JsonRpcMethod("workspace/configuration")]
            public List<object> OnWorkspaceConfigurationRequest(JToken request)
            {
                WorkspaceConfigurationRequest config = request.ToObject<WorkspaceConfigurationRequest>();

                if(config.Items[0].Section == "graphql-config")
                {

                    return new List<object> {  new Dictionary<string, object>
                            {
                                {
                                    "load",
                                    new Dictionary<string, object>
                                    {
                                        { "rootDir", RootDirectory }
                                    }
                                }
                            } };
                       
                }

                return new List<object>();
            }
        }
    }
}
