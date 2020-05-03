
using System;
using HotChocolate;
using NetTopologySuite.Geometries;

namespace Filtering.Customization
{
    public class TouristAttraction
    {
        public TouristAttraction(int id, string name, Geometry location)
        {
            Id = id;
            Name = name;
            Location = location;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public Geometry Location { get; set; }
    }
}