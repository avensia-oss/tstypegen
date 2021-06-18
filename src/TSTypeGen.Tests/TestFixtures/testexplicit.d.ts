declare namespace testexplicit {
  interface ExplicitNamespaceTestClass1 {
    prop1: number;
  }

  interface ExplicitNamespaceTestClass2 extends ExplicitNamespaceTestClass1 {
    prop2: number;
    prop3: testexplicit2.ExplicitNamespaceTestEnum;
  }
}
