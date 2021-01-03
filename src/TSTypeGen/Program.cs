using Microsoft.Build.Locator;
using Mono.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TSTypeGen
{
    class Program
    {
        public const string GenerateTypeScriptDefinitionAttributeName = "GenerateTypeScriptDefinitionAttribute";
        public const string GenerateTypeScriptNamespaceAttributeName = "GenerateTypeScriptNamespaceAttribute";
        public const string GenerateDotNetTypeNamesAsJsDocCommentAttributeName = "GenerateDotNetTypeNamesAsJsDocCommentAttribute";
        public const string DefineTypeScriptTypeForExternalTypeAttributeName = "DefineTypeScriptTypeForExternalTypeAttribute";
        public const string TypeScriptTypeAttributeName = "TypeScriptTypeAttribute";
        public const string TypeScriptOptionalAttributeName = "TypeScriptOptionalAttribute";
        public const string TypeScriptIgnoreAttributeName = "TypeScriptIgnoreAttribute";
        public const string GenerateTypeScriptDerivedTypesUnionAttributeName = "GenerateTypeScriptDerivedTypesUnionAttribute";
        public const string GenerateTypeScriptTypeMemberAttributeName = "GenerateTypeScriptTypeMemberAttribute";
        public const string GenerateTypeScriptTypeConstEnumAttributeName = "GenerateTypeScriptConstEnumAttribute";
        public const string GenerateTypeScriptDotNetNameAttributeName = "GenerateTypeScriptDotNetNameAttribute";

        public const string DefaultTypeMemberName = "$type";

        public static void WriteError(string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }

        public static void WriteWarning(string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }

        static int MainTask(Task<bool> task)
        {
            try
            {
                return task.Result ? 0 : 1;
            }
            catch (AggregateException ex)
            {
                WriteError(ex.InnerException.Message);
                return 1;
            }
            catch (Exception ex) {
                WriteError(ex.Message);
                return 1;
            }
        }

        static int Main(string[] args)
        {
            string solutionOrProjectPath = null, configPath = null, msBuildVersion = null, msBuildPath = null;
            bool showHelp = false, verifyOnly = false, watch = false;
            bool? isProject = null;

            if (!MSBuildHelper.TryQueryMSBuildMajorVersions(out var availableMSBuildMajors))
            {
                Console.Error.WriteLine("Unable to find MSBuild, aborting.");
                return 1;
            }

            var options = new OptionSet
            {
                { "s|sln=", "Path to solution file that contains the projects to process", s => { solutionOrProjectPath = Path.GetFullPath(s); isProject = false; }},
                { "p|prj=", "Path to project file that contains the project to process", p => { solutionOrProjectPath = Path.GetFullPath(p); isProject = true; }},
                { "m|msbuild=", "Major version of MSBuild to use, allowed values: " + availableMSBuildMajors, m => { msBuildVersion = m; }},
                { "b|msbuildpath=", "Path to discovery of MSBuild location: ", b => { msBuildPath = Path.GetFullPath(b); }},
                { "v|verify", "Verify that the generated types are as expected, and return a non-zero exit code if they do not match. Nothing will be updated in this mode.", _ => verifyOnly = true },
                { "w|watch", "Watch the solution for changes and automatically update generated types", _ => watch = true },
                { "c|cfg=", "Specify a file that contains configuration", s => configPath = s },
                { "h|?|help", "Show this message and exit", _ => showHelp = true },
            };

            options.Parse(args);

            if (showHelp || args.Length == 0)
            {
                Console.WriteLine("Updates TypeScript type definitions based on project conventions.");
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (isProject == null || string.IsNullOrEmpty(solutionOrProjectPath))
            {
                Console.Error.WriteLine("Either a solution file (-sln option) or a project file (-prj option) must be specified.");
                return 1;
            }

            if (isProject.Value)
            {
                if (!string.Equals(Path.GetExtension(solutionOrProjectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("Project file must have extension .csproj.");
                    return 1;
                }
            }
            else
            {
                if (!string.Equals(Path.GetExtension(solutionOrProjectPath), ".sln", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("Solution file must have extension .sln.");
                    return 1;
                }
            }

            if (!File.Exists(solutionOrProjectPath))
            {
                Console.Error.WriteLine($"{(isProject.Value ? "Project" : "Solution")} file {solutionOrProjectPath} does not exist.");
                return 1;
            }

            if (string.IsNullOrEmpty(configPath))
            {
                Console.Error.WriteLine("Missing config file (-cfg option).");
                return 1;
            }

            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine($"Config file {configPath} does not exist.");
                return 1;
            }

            if (!string.IsNullOrEmpty(msBuildVersion) && !string.IsNullOrEmpty(msBuildPath))
            {
                Console.Error.WriteLine("Cannot specify both MSBuildVersion and MSBuildPath.");
                return 1;
            }

            if (!string.IsNullOrEmpty(msBuildVersion))
            {
                if (!MSBuildHelper.TryRegisterMSBuildVersion(msBuildVersion))
                {
                    Console.Error.WriteLine($"Unable to register MSBuild version {msBuildVersion}.");
                    return 1;
                }
            } else if (!string.IsNullOrEmpty(msBuildPath))
            {
                if (!MSBuildHelper.TryRegisterMSBuildPath(msBuildPath))
                {
                    Console.Error.WriteLine($"Unable to register MSBuild path {msBuildPath}.");
                    return 1;
                }
            }
            else
            {
                MSBuildLocator.RegisterDefaults();
            }

            var config = ReadConfig(configPath);
            if (config == null)
            {
                return 1;
            }

            var processor = new Processor(solutionOrProjectPath, config);

            if (verifyOnly && watch)
            {
                Console.Error.WriteLine("The -watch and -verify options are incompatible.");
                return 1;
            }

            if (watch)
            {
                return MainTask(processor.WatchAsync(CancellationToken.None));
            }
            else if (verifyOnly)
            {
                return MainTask(processor.VerifyTypesAsync());
            }
            else
            {
                return MainTask(processor.UpdateTypesAsync());
            }
        }

        private static Config ReadConfig(string configPath) {
            try
            {
                return Config.ReadFromFile(configPath);
            }
            catch (Exception ex) {
                WriteError($"Error reading config file {configPath}: {ex.Message}.");
                return null;
            }
        }
    }
}
