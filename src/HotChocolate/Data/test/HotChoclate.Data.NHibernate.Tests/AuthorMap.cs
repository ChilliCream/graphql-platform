// <copyright file="AuthorMap.cs" company="GEODIS">
// Copyright (c) 2021 GEODIS.
// All rights reserved. www.geodis.com
// Reproduction or transmission in whole or in part, in any form or by
//  any means, electronic, mechanical or otherwise, is prohibited without the
//  prior written consent of the copyright owner.
// </copyright>
// 
// <application>All</application>
// <module>HotChoclate.Data.NHibernate.Tests</module>
// <author>Viswanathan, Satish</author>
// <createddate>2021-09-01</createddate>
// <lastchangedby>Viswanathan, Satish</lastchangedby>
// <lastchangeddate>2021-09-01</lastchangeddate>

namespace HotChocolate.Data
{
    using FluentNHibernate.Mapping;

    public class AuthorMap : ClassMap<Author>
    {
        public AuthorMap()
        {
            Id(x => x.Id);
            Map(x => x.Name);
            HasMany(x => x.Books);
            Table("Author");
        }
    }
}
