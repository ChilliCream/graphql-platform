using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    internal interface IAction
    {
        Block CreateBlock();
    }

    internal enum Actions
    {
        Parsing,
        Validation,
        Execution
    }
}
