using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public struct TestOptionalsStruct
    {
    }

    [GenerateTypeScriptDefinition]
    public class TestOptionalsClass
    {
        public byte? Prop1 { get; set; }
        public sbyte? Prop2 { get; set; }
        public short? Prop3 { get; set; }
        public ushort? Prop4 { get; set; }
        public int? Prop5 { get; set; }
        public uint? Prop6 { get; set; }
        public long? Prop7 { get; set; }
        public ulong? Prop8 { get; set; }
        public float? Prop9 { get; set; }
        public double? Prop10 { get; set; }
        public decimal? Prop11 { get; set; }
        public bool? Prop12 { get; set; }
        public TestOptionalsStruct? Prop13 { get; set; }
        public IEnumerable<TestOptionalsStruct?> Prop14 { get; set; }
        public Dictionary<int, TestOptionalsStruct?> Prop15 { get; set; }
        public DateTime? Prop16 { get; set; }
    }
}
