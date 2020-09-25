using System;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4j
{
    public class BoltClient : IDisposable
    {
        private readonly string URI;
        private readonly string UserName;
        private readonly string Password;
        private readonly bool StripHyphens;
        private IDriver Driver;

        public bool IsConnected => Driver != null;

        public BoltClient(
            string uri,
            string userName = null,
            string password = null)
        {
            this.URI = uri;
            this.UserName = userName;
            this.Password = password;
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            Driver = GraphDatabase.Driver(
                URI,
                (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password)) ? null : AuthTokens.Basic(UserName, Password),
                this.Config);
        }

        public void Dispose()
        {
            this.Driver = null;
        }


        public static async Task NewSession(Func<ISession, Task> statement)
        {
            using (var driver = GraphDatabase.Driver(Url))
            {
                using (var session = driver.Session())
                {
                    await statement(session);
                }
            }
        }



        public static async Task NewSessionAsync(Func<IAsyncSession, Task> statement)
        {
            using (var driver = GraphDatabase.Driver(Url))
            {
                var session = driver.AsyncSession();
                await statement(session);
                await session.CloseAsync();
            }
        }


        public static async Task<T> NewSessionAsync<T>(Func<IAsyncSession, Task<T>> statement)
        {
            using (var driver = GraphDatabase.Driver(Url))
            {
                var session = driver.AsyncSession();
                var result = await statement(session);
                await session.CloseAsync();
                return result;
            }
        }
    }
}
