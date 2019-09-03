using System;
using System.Collections.Generic;
namespace HotChocolate.Server.Template
{
    public class Query
    {
        //this could be a database method that hydrates your entire object tree
        //now go to QueryType.cs
        public IEnumerable<GreetingsModel> GetGreetings()
        {
            var greeting = new GreetingsModel("world", "A greeting", 0);
            var greeting1 = new GreetingsModel("world", "A second greeting", 1);
            var ret = new List<GreetingsModel>();
            ret.Add(greeting);
            ret.Add(greeting1);
            return ret; 
        }
    }
}

