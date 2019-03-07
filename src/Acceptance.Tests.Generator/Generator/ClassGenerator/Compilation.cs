using System.Collections.Generic;

namespace Generator.ClassGenerator
{
    public class Compilation
    {
        public string Source { get; set; }
        public List<string> Errors { get; set; }
    }
}
