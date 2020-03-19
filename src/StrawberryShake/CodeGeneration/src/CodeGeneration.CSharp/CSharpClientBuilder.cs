using System;
using System.Collections.Generic;
using System.Text;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpClientBuilder
    {
        public CSharpClientBuilder SetModel(ClientModel model)
        {
            return this;
        }

        private static ClientClassDescriptor CreateClientClassDescriptor(ClientModel model)
        {
            var descriptor = new ClientClassDescriptor(
                "TestClient",
                "Demo",
                "ITestClient",
                "global::StrawberryShake.IOperationExecutorPool",
                "global::StrawberryShake.IOperationExecutor",
                "global::StrawberryShake.IOperationStreamExecutor",
                new[]
                {
                                new ClientOperationMethodDescriptor(
                                    "GetHero",
                                    "global::Demo.GetHeroOperation",
                                    false,
                                    "global::Demo.IGetHero",
                                    new[]
                                    {
                                        new ClientOperationMethodParameterDescriptor(
                                            "episode",
                                            "Episode",
                                            "global::Demo.Episode",
                                            true,
                                            "global::Demo.Episode.NewHope")
                                    })
                });
        }       
    }
}
