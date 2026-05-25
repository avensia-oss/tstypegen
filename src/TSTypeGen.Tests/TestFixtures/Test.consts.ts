export const TestConstEnum = {
  FirstValue: 'firstValue',
  SecondValue: 'secondValue',
  ThirdValue: 'thirdValue',
} as const;
export type TestConstEnum = (typeof TestConstEnum)[keyof typeof TestConstEnum];

export const TestOptionalPropertiesEnum = {
  Value1: 'value1',
  Value2: 'value2',
} as const;
export type TestOptionalPropertiesEnum = (typeof TestOptionalPropertiesEnum)[keyof typeof TestOptionalPropertiesEnum];
