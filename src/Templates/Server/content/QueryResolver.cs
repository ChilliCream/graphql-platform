using System;
using HotChocolate.Resolvers;

namespace HotChocolate.Server.Template
{
    public class QueryResolver
    {
        //add your depencency injected class in the construtor to access it 
        public string GetMessageBasedOnIndex([Parent] GreetingsModel hello)
        {
            return $"Index is {hello.Index}";
        }

        // put a data loader here https://hotchocolate.io/docs/dataloaders 
    }
}
