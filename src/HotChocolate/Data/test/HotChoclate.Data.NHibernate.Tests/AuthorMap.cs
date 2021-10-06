using FluentNHibernate.Mapping;

namespace HotChocolate.Data
{
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
