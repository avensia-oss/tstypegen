using System;
using System.Collections.Generic;
using System.Text;
using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    /// <typeScriptComment>
    /// This type is awesome!
    /// </typeScriptComment>
    [GenerateTypeScriptDefinition]
    public abstract class TestClassWithComments
    {
        /// <typeScriptComment>
        /// This is the best property you've ever seen!
        /// This is a comment on a new line.
        ///
        /// Wow, this is a comment with an empty line above it!
        /// </typeScriptComment>
        public int Prop1 { get; set; }
        /// <typeScriptComment>
        ///
        /// This comment
        ///   has a bit
        ///  odd whitespace
        ///
        /// formatting. With whitespace at the end.
        ///
        ///
        /// </typeScriptComment>
        public int Prop2 { get; set; }
        /// <summary>
        /// This is a regular summary comment
        /// </summary>
        public int Prop3 { get; set; }
        /// <summary>
        /// This is a regular summary comment
        /// </summary>
        /// <typeScriptComment>
        /// This is a typeScriptComment for a property that also has a summary comment
        /// </typeScriptComment>
        public int Prop4 { get; set; }
        /// <summary>
        /// An empty typeScriptComment prevents this summary from showing up
        /// </summary>
        /// <typeScriptComment>
        /// </typeScriptComment>
        public int Prop5 { get; set; }
        public int Prop6 { get; set; }
    }

    /// <summary>
    /// This is a comment on the interface
    /// </summary>
    public interface IInterfaceWithComments
    {
        /// <summary>
        /// This is an interface comment
        /// </summary>
        public int Prop1 { get; set; }
    }

    [GenerateTypeScriptDefinition]
    public class TestClassWithInterface : IInterfaceWithComments
    {
        public int Prop1 { get; set; }
    }
}
