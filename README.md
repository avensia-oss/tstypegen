[![.NET](https://github.com/avensia-oss/tstypegen/actions/workflows/dotnet.yml/badge.svg)](https://github.com/avensia-oss/tstypegen/actions/workflows/dotnet.yml)

This program will generate TypeScript definitions from C# types. It's built on .NET Core which makes it possible to use on Windows, Mac and Linux.

## Install

TSTypeGen is distributed as an npm package, install by doing:

```
yarn add @avensia-oss/tstypegen
```

## Usage

`dotnet node_modules/@avensia-oss/tstypegen/bin/TSTypeGen.dll -c <config-file> [-verify]`

Options:

- `-c`: Specify the configuration file (see below for format)
- `-verify`: Do not update any definitions but instead return an error if any files need updates. This can be used on a CI server to detect if all types have been built and checked in.

In order to generate a file, the minimum you need to do is define a C# attribute called `GenerateTypeScriptDefinitionAttribute`, like this:

```cs
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

After that you can add that attribute to the classes etc that you want to generate TypeScript types for. TSTypeGen will see your attribute regardless even if it's defined in your code as it will match based on the attribute name.

## Config file

The config file is a JSON with the following options:

- `newLine`: The character(s) to use to generate new lines, defaults to "\n".
- `basePath`: Optional base path for where to find files. If omitted the base path becomes the path to the config file.
- `outputPath`: Optional path to where generated d.ts-files are placed. If omitted the output path becomes the base path.
- `dllPatterns`: An array of glob patterns to find dll files to inspect.
- `typeMappings`: A dictionary that maps type full C# names (including namespaces) to corresponding TypeScript types.
- `customTypeScriptIgnoreAttributeFullName`: Name of a custom attribute to treat the same as `[JsonIgnore]` and `[TypeScriptIgnore]`
- `propertyWrappers`: A dictionary that defines wrappers for properties in the listed types

Example:

```
{
  newLine: "\n",
  basePath: "./src",
  outputPath: "./ts-types",
  dllPatterns: ["src/bin/Debug/**/*.dll"],
  typeMappings: {
    "Cms.CmsData": "@avensia/core/CmsData",
    "System.DateTime": "string",
    "System.Guid": "string",
  },
  customTypeScriptIgnoreAttributeFullName: "Avensia.MyTypeScriptIgnoreAttribute",
}
```

## Controlling types with attributes

TSTypeGen comes with multiple attributes that lets you control how the TypeScript types are created. All of them should be defined in your code as TSTypeGen will identify them by name.

### `[GenerateTypeScriptNamespace("Backend")]`

This attribute is typically placed in `AssemblyInfo.cs` and it will control the name of the namespace and .d.ts-file that the TypeScript types are placed in.

### `[TypeScriptType("MyType")]`

Use this to override the type that TSTypeGen selects for a property. You should most likely only do this if you also make the same change in how you serialize the C# objects to JSON.

Example:

```cs
[GenerateTypeScriptDefinition]
public class MyApiModel
{
  [TypeScriptType("string")]
  public Guid Id { get; set; }
}
```

### `[TypeScriptOptional]`

Use this to make TSTypeGen generate a property as optional.

Example:

```cs
[GenerateTypeScriptDefinition]
public class MyApiModel
{
  [TypeScriptOptional]
  public string Id { get; set; }
}
```

becomes:

```ts
interface MyApiModel {
  id?: string;
}
```

### `[GenerateTypeScriptDerivedTypesUnion]`

Use this on an abstract base class to generate a union type of all derived child types. This is best used in combination with `[GenerateTypeScriptTypeMember]` and comes in handy when the TypeScript code needs to create command objects of a limited set of allowed types.

Example:

```cs
[GenerateTypeScriptDefinition]
[GenerateTypeScriptTypeMember]
[GenerateTypeScriptDerivedTypesUnion]
public abstract class Command
{

}

[GenerateTypeScriptDefinition]
public class UpdateCommand : Command
{
  public string Id { get; set; }
  public string Name { get; set; }
}

[GenerateTypeScriptDefinition]
public class InsertCommand : Command
{
  public string Name { get; set; }
}
```

becomes:

```ts
interface Command {}

interface UpdateCommand extends Command {
  $type: "UpdateCommand";
  id: string;
  name: string;
}

interface InsertCommand extends Command {
  $type: "InsertCommand";
  name: string;
}

type CommandTypes = UpdateCommand | InsertCommand;
```

Now you can have a method that takes `CommandTypes` as an argument and you can know that only valid command objects are passed to it.

### `[GenerateTypeScriptTypeMember]`

Use this to inject a `$type` property with the class name. Best used in combination with `[GenerateTypeScriptDerivedTypesUnion]`. See above example.

### `[GenerateDotNetTypeNamesAsJsDocComment]`

This adds a JSDoc comment to the TypeScript type with the full .NET name of the type. This can be useful if you want to use the TypeScript compiler API to find the full .NET name for a certain type. Example:

```cs
[assembly: GenerateDotNetTypeNamesAsJsDocComment]

public namespace MyNamespace
{
  [GenerateTypeScriptDefinition]
  public class MyApiModel
  {
    public string Id { get; set; }
  }
}
```

becomes:

```ts
/** @DotNetTypeName MyNamespace.MyApiModel,MyNamespace */
interface MyApiModel {
  id: string:
}
```

### `[GenerateCanonicalDotNetTypeScriptType]`

This is used together with `[GenerateDotNetTypeNamesAsJsDocComment]` and lets you add an additional JSDoc comment to any class that implements an interface that has a `[GenerateCanonicalDotNetTypeScriptType]` attribute. An example:

```cs
[assembly: GenerateDotNetTypeNamesAsJsDocComment]

public namespace MyNamespace
{
  [GenerateTypeScriptDefinition]
  [GenerateCanonicalDotNetTypeScriptType]
  public interface IMyApiModel
  {
    string Id { get; set; }
  }

  [GenerateTypeScriptDefinition]
  public class MyApiModel : IMyApiModel
  {
    public string Id { get; set; }
  }
}
```

becomes:

```ts
interface IMyApiModel {
  id: string:
}

/**
 * @DotNetTypeName MyNamespace.MyApiModel,MyNamespace
 * @DotNetCanonicalTypeName MyNamespace.IMyApiModel,MyNamespace
 */
interface MyApiModel {
  id: string:
}
```

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
