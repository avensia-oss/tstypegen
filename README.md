This program will generate TypeScript definitions from C# types. It's built with Roslyn on .NET Core which makes it possible to use on Windows, Mac and Linux.

## Install

TSTypeGen is distributed as an npm package, install by doing:
```
yarn add @avensia-oss/tstypegen
```

## Usage

`TSTypeGen.exe -cfg <config-file> -p <project-file> -sln <solution-file> [-watch|-verify]`

Options:
* `-cfg`: Specify the configuration file (see below for format)
* `-sln`: Path to an `.sln` file to process (can be omitted if you specify `-p`)
* `-p`: Path to a `.csproj` file to process (can be omitted if you specify `-sln`)
* `-watch`: Keep running and update types as C# files are modified.
* `-verify`: Do not update any definitions but instead return an error if any files need updates.

The `TSTypeGen.exe` file in the npm package is built for Windows, but the package also contains a `TSTypeGen.dll` that is cross-platform and can be executed with `dotnet run node_modules/@avensia-oss/tstypegen/bin/TSTypeGen.dll`.

In order to generate a file, the minimum you need to do is define a C# attribute called `GenerateTypeScriptDefinitionAttribute`, like this:
```
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum)]
public sealed class GenerateTypeScriptDefinitionAttribute : Attribute
{
    public bool Generate { get; }

    public GenerateTypeScriptDefinitionAttribute()
    {
        Generate = true;
    }

    public GenerateTypeScriptDefinitionAttribute(bool generate)
    {
        Generate = generate;
    }
}
```

After that you can add that attribute to the classes etc that you want to generate TypeScript types for. TSTypeGen will see your attribute regardless even if it's defined in your code.

Read more about additional attributes below.

## Config file

The config file is a JSON with the following options:

* basePath: Optional base path for where to find files. If omitted the base path becomes the path to the config file.
* typeMappings: A dictionary that maps type full C# names (including namespaces) to corresponding TypeScript files or types.
* propertyTypeDefinitionFile: An optional file that defines the Property<T> type used to wrap C# properties. Read more about property wrapping below.
* propertyTypeName: An optional global type used to wrap C# properties. Read more about property wrapping below.
* typesToWrapPropertiesFor: An optional array of full C# names (including namespaces) for types for which properties should be wrapped.
* pathAliases: An optional dictionary that maps paths to aliases.
* rootPath: An optional alternate root path for the output files.
* useConstEnums: Wether to generate C# enums as const enums or string unions. Defaults to true.
* useOptionalForNullables: Wether to generate C# nullables as optional in TypeScript. Defaults to true. 

Example:
```
{
  generationAttributeName: "Avensia.Scope.GenerateTypeScriptDefinitionAttribute",
  typeMappings: {
    "Cms.CmsData": "@avensia/core/CmsData",
    "System.DateTime": "string",
    "System.Guid": "string",
  },
  propertyTypeName: "Avensia.Property",
  propertyTypeDefinitionFile: "@avensia/Property",
  typesToWrapPropertiesFor: ["Cms.CmsData", "Avensia.IPartialContentData"],
  pathAliases: {
    "@avensia/core": "@avensia/core",
    "ContentTypes": "src/Avensia.Site/ContentTypes"
  },
  rootPath: "src/Avensia.Site/Features"
}
```

## Controlling types with attributes

TSTypeGen comes with multiple attributes that lets you control how the TypeScript types are created. All of them should be defined in your code, and TSTypeGen will identify them by name.

### `[GenerateTypeScriptNamespace("Backend")]`

Use this attribute if you want to generate one `.d.ts` file with a TypeScript namespace with all type definitions instead of one `.type.ts` file per C# type. This attribute is typically placed in `AssemblyInfo.cs`.

### `[TypeScriptType("MyType")]`

Use this to override the type that TSTypeGen selects for a property. You should most likely only do this if you also make the same change in how you serialize the C# objects to JSON.

### `[TypeScriptOptional]`

Use this to make TSTypeGen generate a property as optional.

### `[GenerateTypeScriptDerivedTypesUnion]`

Use this on an abstract base class to generate a union type of all derived child types. This is best used in combination with `[GenerateTypeScriptTypeMember]` and comes in handy when the TypeScript code needs to create command objects of a limited set of allowed types. See unit tests for details.

### `[GenerateTypeScriptTypeMember]`

Use this to inject a `$type` property with the class name. Best used in combination with `[GenerateTypeScriptDerivedTypesUnion]`. See unit tests for details.

## Contributing

Please create an issue for discussion before starting to work on something. Contributions are very welcome!

### Creating a release

If you have gotten publishing rights to the npm package, you first run:
```
dotnet test ./src/TSTypeGen.Tests/TSTypeGen.Tests.csproj
```
to make sure the tests still pass. Then you run:
```
dotnet publish src/TSTypeGen/TSTypeGen.csproj -o ./bin
```
which creates a `bin/` folder in the root with the exe and dll files. After that you run:
```
npm publish
```

## Additional details

The unit tests are quite extensive and written in a way that should make it quite clear how TSTypeGen works and the different configuration options there is.