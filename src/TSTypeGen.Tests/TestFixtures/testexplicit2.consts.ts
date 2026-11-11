export const ExplicitNamespaceTestEnum = {
  FirstValue: 'firstValue',
  SecondValue: 'secondValue',
  ThirdValue: 'thirdValue',
} as const;
export type ExplicitNamespaceTestEnum = (typeof ExplicitNamespaceTestEnum)[keyof typeof ExplicitNamespaceTestEnum];
