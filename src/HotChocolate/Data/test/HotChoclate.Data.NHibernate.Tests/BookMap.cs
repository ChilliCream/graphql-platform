

namespace HotChocolate.Data
{
    using FluentNHibernate.Mapping;

    public class BookMap : ClassMap<Book>
    {


        public BookMap()
        {
            Id(x => x.Id).Column("Id");
            HasOne(x => x.Author).Cascade.All().PropertyRef(x=> x.Id);
            Map(x => x.AuthorId);
            Map(x => x.Title);
            Table("Book");
        }

      
     
    }
}
