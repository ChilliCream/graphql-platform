using System;
using System.Reflection;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace HotChocolate.Data
{
    public class InMemoryDatabaseTest : IDisposable
    {
        private static NHibernate.Cfg.Configuration _configuration;
        private static ISessionFactory _sessionFactory;
        protected ISession Session;

        public InMemoryDatabaseTest(Assembly assemblyContainingMapping)
        {
            if (_configuration == null)
            {
                FluentConfiguration fluentConfig = Fluently.Configure()
                    .Database(SQLiteConfiguration.Standard.InMemory);

                _configuration = fluentConfig.Mappings(
                        cfg =>
                        {
                            cfg.FluentMappings.AddFromAssembly(assemblyContainingMapping);
                        })
                    
                    .BuildConfiguration();
                _sessionFactory = _configuration.BuildSessionFactory();
            }

            Session = _sessionFactory.OpenSession();
            new SchemaExport(_configuration).Execute(true, true, false, Session.Connection, Console.Out);
        }

        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
