using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class GenericsTestBase<T>
    {
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class GenericsTestClass1<T1, T2>
    {
    }

    [GenerateTypeScriptDefinition]
    public class GenericsTestClass2
    {
    }

    [GenerateTypeScriptDefinition]
    public interface GenericsITest1<T1, T2>
    {
        T1 Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public interface GenericsITest2<T> : GenericsITest1<int, T>
    {
    }

    [GenerateTypeScriptDefinition]
    public class GenericsTestClass3<T> : GenericsTestBase<GenericsITest1<int, GenericsITest2<T>>>
    {
    }

    [GenerateTypeScriptDefinition]
    public class GenericsTestClass4
    {
        public GenericsITest1<GenericsTestClass1<GenericsTestClass2, string>, GenericsTestClass3<GenericsITest2<GenericsTestClass3<string>>>> Prop1 { get; set; }
    }
}
