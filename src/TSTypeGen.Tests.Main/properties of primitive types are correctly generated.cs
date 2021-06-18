using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestPrimitiveTypesClass
    {
        public byte Prop1 { get; set; }
        public sbyte Prop2 { get; set; }
        public short Prop3 { get; set; }
        public ushort Prop4 { get; set; }
        public int Prop5 { get; set; }
        public uint Prop6 { get; set; }
        public long Prop7 { get; set; }
        public ulong Prop8 { get; set; }
        public float Prop9 { get; set; }
        public double Prop10 { get; set; }
        public decimal Prop11 { get; set; }
        public string Prop12 { get; set; }
        public bool Prop13 { get; set; }
    }
}
