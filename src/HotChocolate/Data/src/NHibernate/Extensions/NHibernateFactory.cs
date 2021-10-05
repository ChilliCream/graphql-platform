// <copyright file="NHibernateFactory.cs" company="GEODIS">
// Copyright (c) 2021 GEODIS.
// All rights reserved. www.geodis.com
// Reproduction or transmission in whole or in part, in any form or by
//  any means, electronic, mechanical or otherwise, is prohibited without the
//  prior written consent of the copyright owner.
// </copyright>
// 
// <application>All</application>
// <module>HotChoclate.Data.NHibernate</module>
// <author>Viswanathan, Satish</author>
// <createddate>2021-09-02</createddate>
// <lastchangedby>Viswanathan, Satish</lastchangedby>
// <lastchangeddate>2021-09-02</lastchangeddate>

namespace HotChoclate.Data
{
    using System;
    using System.Reflection;
    using FluentNHibernate.Cfg;
    using FluentNHibernate.Cfg.Db;
    using NHibernate;
    using NHibernate.Cfg;
    using NHibernate.Tool.hbm2ddl;
    using Microsoft.Extensions.DependencyInjection;

    public static class NHibernateFactory
    {
        /// <summary>
        /// Configures NHibernate session for the configuration.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="assemblyContainingMapping"></param>
        /// <param name="config"></param>
        /// <param name="createSchema">Exports schema.(Not for production use) </param>
        /// <returns></returns>
        public static IServiceCollection AddNHibernateFactory(this IServiceCollection serviceCollection,
            Assembly assemblyContainingMapping, Func<IPersistenceConfigurer> config, bool createSchema= false)
        {
            FluentConfiguration fluentConfig = Fluently.Configure().Database(config);
            Configuration configuration = fluentConfig.Mappings(cfg =>
            {
                cfg.FluentMappings.AddFromAssembly(assemblyContainingMapping);
            })
                .ExposeConfiguration(cfg => new SchemaExport(cfg).Create(true, true))
                .BuildConfiguration();

           ISessionFactory sessionFactory = configuration.BuildSessionFactory();
           serviceCollection.AddSingleton(_ => sessionFactory);


           if (createSchema)
           {
               ISession session = sessionFactory.OpenSession();
               new SchemaExport(configuration).Execute(true, true, false, session.Connection, Console.Out);

               serviceCollection.AddSingleton(_ => session);
           }
           else
           {

               serviceCollection.AddScoped(_ =>
               {
                   ISessionFactory factory = _.GetRequiredService<ISessionFactory>();
                   return factory
                       .WithOptions()
                       .Interceptor(new AuditTrailInterceptor())
                       .OpenSession();
               });
           }

           return serviceCollection;
        }
    }
}
