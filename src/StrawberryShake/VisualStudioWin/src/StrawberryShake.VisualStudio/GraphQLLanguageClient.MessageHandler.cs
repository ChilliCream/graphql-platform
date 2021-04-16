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
                                        { "rootDir", @"C:\Users\michael\source\repos\config" }
                                    }
                                }
                            } };
                       
                }

                return new List<object>();


                // var dte = (EnvDTE.DTE)ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE));
                // dte.Windows.Ac

                // object activeSolutionProject = dte.SelectedItems.Item(1);
                // var project = activeSolutionProject as EnvDTE.SelectedItem;

                //return new Dictionary<string, string>();
            }
        }
    }
}
