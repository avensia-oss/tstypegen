using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [Obsolete]
    [GenerateTypeScriptDefinition]
    public class TestGenerateObsoleteClass
    {
        [Obsolete]
        public int Prop1 { get; set; }
    }

    [Obsolete("Don't use this ok?")]
    [GenerateTypeScriptDefinition]
    public class TestGenerateObsoleteClassWithComment
    {
        [Obsolete("If you use this you'll be fired")]
        public int Prop1 { get; set; }
    }

    /// <summary>
    /// This should not be used
    /// </summary>
    [Obsolete("Don't use this ok?")]
    [GenerateTypeScriptDefinition]
    public class TestGenerateObsoleteClassWithCommentAndSummary
    {
        /// <summary>
        /// Don't use this ok?
        /// </summary>
        [Obsolete("If you use this you'll be fired")]
        public int Prop1 { get; set; }
    }

    /// <summary>
    /// This should not be used at all
    /// </summary>
    [Obsolete("Please don't use this ok?")]
    [GenerateTypeScriptDefinition]
    [GenerateDotNetTypeNamesAsJsDocComment]
    public class TestGenerateObsoleteClassWithCommentAndSummaryAndDotNetType
    {
        /// <summary>
        /// Don't use this ok?
        /// </summary>
        [Obsolete("If you use this you'll be fired!")]
        public int Prop1 { get; set; }
    }
}
