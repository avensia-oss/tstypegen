using TSTypeGen.Tests.Shared;

namespace TSTypeGen.Tests.Main
{
    [GenerateTypeScriptDefinition]
    public class TestOnlyInstancePropertiesGeneratedClass
    {
        public int Prop1 { get; set; }
        public void Method1() { }
        public int Field1;
#pragma warning disable CS0067
        public event System.EventHandler Event1;
#pragma warning restore CS0067

        public static int Prop2 { get; set; }
        public static void Method2() { }
        public static int Field2;
#pragma warning disable CS0067
        public static event System.EventHandler Event2;
#pragma warning restore CS0067

        public int this[int x] { get { return x; } set { } }
    }
}
