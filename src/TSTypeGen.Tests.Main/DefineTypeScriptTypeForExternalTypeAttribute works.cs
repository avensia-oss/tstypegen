﻿using System;
using System.Collections.Generic;
using TSTypeGen.Tests.Shared;

[assembly: DefineTypeScriptTypeForExternalType(typeof(System.TimeSpan), "external.CustomTimeSpan")]
[assembly: DefineTypeScriptTypeForExternalType(typeof(System.DateTime), "external.CustomDateTime")]
[assembly: DefineTypeScriptTypeForExternalType(typeof(List<byte>), "external.CustomByteList")]
[assembly: DefineTypeScriptTypeForExternalType("System.Guid", "external.CustomGuid")]

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class DefineTypeScriptTypeForExternalTypeTestClass1
    {
        public DateTime Prop1 { get; set; }
        public TimeSpan Prop2 { get; set; }
        public Guid Prop3 { get; set; }
        public List<byte> Prop4 { get; set; }
    }
}