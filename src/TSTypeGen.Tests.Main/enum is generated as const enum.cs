using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public enum TestConstEnum
    {
        FirstValue,
        SecondValue,
        ThirdValue
    }
}
