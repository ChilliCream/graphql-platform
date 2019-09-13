using System;

#if ASPNETCLASSIC
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Voyager
#else
namespace HotChocolate.AspNetCore.Voyager
#endif
{
    public class VoyagerOptions
    {
        private bool _pathIsSet;
        private PathString _path = new PathString("/voyager");
        private PathString _queryPath = new PathString("/");

        public PathString Path
        {
            get => _path;
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentException(
                        "The path cannot be empty.");
                }

                _path = value;
                _pathIsSet = true;
            }
        }

        public PathString QueryPath
        {
            get => _queryPath;
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentException(
                        "The query-path cannot be empty.");
                }

                _queryPath = value;

                if (!_pathIsSet)
                {
                    _path = value.Add(new PathString("/voyager"));
                }
            }
        }
    }
}
