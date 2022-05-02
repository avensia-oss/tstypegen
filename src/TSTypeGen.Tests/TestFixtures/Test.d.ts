declare namespace Test {
  interface DefineTypeScriptTypeForExternalTypeTestClass1 {
    prop1: external.CustomDateTime;
    prop2: external.CustomTimeSpan;
    prop3: external.CustomGuid;
    prop4: external.CustomByteList;
  }

  interface ExplicitNamespaceModuleClass {
    prop: number;
  }

  interface GenericsITest1<T1, T2> {
    prop1: T1;
  }

  interface GenericsITest2<T> extends GenericsITest1<number, T> {
  }

  interface GenericsTestBase<T> {
    prop1: number;
  }

  interface GenericsTestClass1<T1, T2> {
  }

  interface GenericsTestClass2 {
  }

  interface GenericsTestClass3<T> extends GenericsTestBase<GenericsITest1<number, GenericsITest2<T>>> {
  }

  interface GenericsTestClass4 {
    prop1: GenericsITest1<GenericsTestClass1<GenericsTestClass2, string>, GenericsTestClass3<GenericsITest2<GenericsTestClass3<string>>>>;
  }

  interface InnerClass15 {
    prop2: number;
  }

  interface InterfaceImplementationTest {
    prop1: number;
  }

  interface InterfaceImplementationTestClass {
    prop1: number;
    prop2: number;
  }

  interface ITestInterfaceGeneration1 {
    prop1: number;
  }

  interface ITestInterfaceGeneration2 {
    prop2: number;
  }

  interface ITestInterfaceGeneration3 extends ITestInterfaceGeneration1, ITestInterfaceGeneration2 {
    prop3: number;
  }

  interface JsonPropertyInterfaceTestClass {
    InterfaceProp1: string;
    ClassProp2: string;
  }

  /** @DotNetTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeBaseClass2,TSTypeGen.Tests.Main */
  interface TestCanonicalDotNetTypeBaseClass2 {
    prop1: number;
  }

  /**
   * @DotNetTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeBaseClass3,TSTypeGen.Tests.Main
   * @DotNetCanonicalTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeInterface3,TSTypeGen.Tests.Main
   */
  interface TestCanonicalDotNetTypeBaseClass3 {
    prop1: number;
  }

  /**
   * @DotNetTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeClass1,TSTypeGen.Tests.Main
   * @DotNetCanonicalTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeInterface1,TSTypeGen.Tests.Main
   */
  interface TestCanonicalDotNetTypeClass1 {
    prop1: number;
  }

  /**
   * @DotNetTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeClass2,TSTypeGen.Tests.Main
   * @DotNetCanonicalTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeBaseClass2,TSTypeGen.Tests.Main
   */
  interface TestCanonicalDotNetTypeClass2 extends TestCanonicalDotNetTypeBaseClass2 {
    prop2: number;
  }

  /**
   * @DotNetTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeClass3,TSTypeGen.Tests.Main
   * @DotNetCanonicalTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeInterface3,TSTypeGen.Tests.Main
   */
  interface TestCanonicalDotNetTypeClass3 extends TestCanonicalDotNetTypeBaseClass3 {
    prop2: number;
  }

  /** @DotNetTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeInterface1,TSTypeGen.Tests.Main */
  interface TestCanonicalDotNetTypeInterface1 {
    prop1: number;
  }

  /** @DotNetTypeName TSTypeGen.Tests.Main.TestCanonicalDotNetTypeWrapper+TestCanonicalDotNetTypeInterface3,TSTypeGen.Tests.Main */
  interface TestCanonicalDotNetTypeInterface3 {
    prop1: number;
  }

  /**
   * This type is awesome!
   */
  interface TestClassWithComments {
    /**
     * This is the best property you've ever seen!
     * This is a comment on a new line.
     * 
     * Wow, this is a comment with an empty line above it!
     */
    prop1: number;
    /**
     * 
     * This comment
     *   has a bit
     *  odd whitespace
     * 
     * formatting. With whitespace at the end.
     * 
     * 
     */
    prop2: number;
    /**
     * This is a regular summary comment
     */
    prop3: number;
    /**
     * This is a typeScriptComment for a property that also has a summary comment
     */
    prop4: number;
    prop5: number;
    prop6: number;
  }

  const enum TestConstEnum {
    FirstValue = 'firstValue',
    SecondValue = 'secondValue',
    ThirdValue = 'thirdValue',
  }

  interface TestDefaultInterfaceImplementation {
    prop1: number;
  }

  interface TestDefaultInterfaceImplementationClass {
    prop2: number;
    prop1: number;
  }

  interface TestDerivedTypesUnionBase {
    prop1: number;
  }

  type TestDerivedTypesUnionBaseTypes =
    | TestDerivedTypesUnionClass1
    | TestDerivedTypesUnionClass3;

  interface TestDerivedTypesUnionClass1 extends TestDerivedTypesUnionBase {
    prop2: number;
  }

  interface TestDerivedTypesUnionClass2 extends TestDerivedTypesUnionBase {
    prop2: number;
  }

  type MyUnion =
    | TestDerivedTypesUnionClass3;

  interface TestDerivedTypesUnionClass3 extends TestDerivedTypesUnionClass2 {
    prop3: number;
  }

  interface TestDictionariesClass {
    prop1: {[item: string]: string};
    prop2: {[item: number]: boolean};
    prop3: {[item: string]: string};
    prop4: {[item: number]: boolean};
    prop5: {[item: string]: string};
    prop6: {[item: number]: boolean};
  }

  interface TestDisableInnerClass1 {
    prop1: number;
  }

  /** @DotNetTypeName TSTypeGen.Tests.Main.TestDotNetNames+TestDotNetNameClass1,TSTypeGen.Tests.Main */
  interface TestDotNetNameClass1 {
  }

  /** @DotNetTypeName TSTypeGen.Tests.Main.TestDotNetNames+TestDotNetNameClass2,TSTypeGen.Tests.Main */
  interface TestDotNetNameClass2 {
  }

  /** @DotNetTypeName TSTypeGen.Tests.Main.TestDotNetNames+TestDotNetNameClass1,TSTypeGen.Tests.Main */
  interface TestDotNetNameClass3 {
  }

  interface TestEmptyClass {
  }

  interface TestEnumerablePropertiesClass {
    prop1: string[];
    prop2: string[];
    prop3: string[];
    prop4: string[];
    prop5: string[];
  }

  interface TestGenerateFieldsClass1 {
    prop1: 1;
    prop2: 2;
    prop3: 3.3;
    prop4: 'prop4';
  }

  interface TestGenerateTypeMemberBase {
    prop1: number;
  }

  interface TestGenerateTypeMemberClass1 {
    $type: 'TestGenerateTypeMemberClass1';
    prop1: number;
  }

  interface TestGenerateTypeMemberClass2 {
    TheTypeOfThis: 'TestGenerateTypeMemberClass2';
    prop1: number;
  }

  interface TestGenerateTypeMemberClass3 extends TestGenerateTypeMemberBase {
    $type: 'TestGenerateTypeMemberClass3';
    prop2: number;
  }

  interface TestGenericTypeWrapperBase {
    prop1: Wrapper<number>;
  }

  interface TestGenericTypeWrapperBase1 extends TestGenericTypeWrapperBase {
    prop2: Wrapper<string>;
  }

  interface TestGenericTypeWrapperClass1 {
    prop1: Wrapper<number>;
    prop2: Wrapper<string>;
    prop3: Wrapper<TestGenericTypeWrapperClass2>;
  }

  interface TestGenericTypeWrapperClass2 {
    prop1: number;
  }

  interface TestInheritanceBase {
    prop1: number;
  }

  interface TestInheritanceClass extends TestInheritanceBase {
    prop2: number;
  }

  interface TestJsonIgnoreBaseClass {
  }

  interface TestJsonIgnoreChildClass extends TestJsonIgnoreBaseClass {
  }

  interface TestJsonIgnoreClass {
    interfaceProp4: string;
    prop1: string;
  }

  interface TestJsonIgnoreInterface {
    interfaceProp4: string;
  }

  interface TestJsonPropertyClass {
    prop1: string;
    RenamedProp2: string;
    RenamedProp3: string;
    RenamedProp4: string;
    '0IsNotAValidJsIdentifier': string;
  }

  interface TestNestedClasses {
    prop1: number;
  }

  interface TestNestedInnerClass1 {
    prop2: number;
  }

  interface TestNestedInnerClass2 {
    prop3: number;
  }

  interface TestOnlyInstancePropertiesGeneratedClass {
    prop1: number;
  }

  interface TestOptionalPropertiesClass {
    prop1?: number;
    prop2?: string;
    prop3?: string[];
    prop4?: {[item: string]: string};
    prop5?: TestOptionalPropertiesEnum;
    prop6?: TestOptionalPropertiesNestedClass;
  }

  const enum TestOptionalPropertiesEnum {
    Value1 = 'value1',
    Value2 = 'value2',
  }

  interface TestOptionalPropertiesFromInterfaceClass {
    prop1?: number;
  }

  interface TestOptionalPropertiesNestedClass {
    nestedProp1: number;
  }

  interface TestOptionalsClass {
    prop1?: number;
    prop2?: number;
    prop3?: number;
    prop4?: number;
    prop5?: number;
    prop6?: number;
    prop7?: number;
    prop8?: number;
    prop9?: number;
    prop10?: number;
    prop11?: number;
    prop12?: boolean;
    prop13?: TestOptionalsStruct;
    prop14: (TestOptionalsStruct | undefined)[];
    prop15: {[item: number]: TestOptionalsStruct | undefined};
    prop16?: external.CustomDateTime;
  }

  interface TestOptionalsStruct {
  }

  interface TestPrimitiveTypesClass {
    prop1: number;
    prop2: number;
    prop3: number;
    prop4: number;
    prop5: number;
    prop6: number;
    prop7: number;
    prop8: number;
    prop9: number;
    prop10: number;
    prop11: number;
    prop12: string;
    prop13: boolean;
  }

  interface TestStruct1 {
    prop1: number;
  }

  interface TestStruct2 {
    prop1: TestStruct1;
  }

  interface TestTypeScriptStringClass {
    prop1: string;
  }

  interface TestTypeScriptTypeClass1 {
    prop1: string;
    prop2: TestTypeScriptTypeClass2;
    prop3: external.Type;
  }

  interface TestTypeScriptTypeClass2 {
  }

  interface TestTypeScriptTypeOnTypeClass1 extends TypeScriptTypeOnTypeReplacedWith {
    prop1: TypeScriptTypeOnTypeReplacedWith;
    prop2: external.NewClass;
  }

  interface TestUnionFieldsBase {
    prop1: number;
  }

  type TestUnionFieldsBaseTypes =
    | TestUnionFieldsClass1
    | TestUnionFieldsClass2;

  interface TestUnionFieldsClass1 extends TestUnionFieldsBase {
    prop2: number;
  }

  interface TestUnionFieldsClass2 extends TestUnionFieldsBase {
    prop3: number;
  }

  interface TestUnionFieldsMainTestClass {
    prop1: TestUnionFieldsBaseTypes;
    prop2: TestUnionFieldsBaseTypes[];
    prop3: TestUnionFieldsBaseTypes[];
    prop4: {[item: string]: TestUnionFieldsBaseTypes};
  }

  interface TestUsingClassFromOtherProject {
    sharedProp: TestShared.SharedClass;
  }

  interface TypeScriptTypeOnTypeReplacedWith {
  }
}
