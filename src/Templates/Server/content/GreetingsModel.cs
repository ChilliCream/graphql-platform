using System;
namespace HotChocolate.Server.Template
{
    //this could be from a database etc 
    public class GreetingsModel
    {
        public string Hello { get; set; }
        public string Message { get; set; }
        public int Index { get; set; }

        public GreetingsModel(string hello, string message, int status)
        {
            this.Hello = hello;
            this.Message = message;
            this.Index = status;
        }
    }
}
