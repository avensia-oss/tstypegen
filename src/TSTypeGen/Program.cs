using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TSTypeGen
{
    public class Program
    {
        public static void WriteError(string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
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
            catch (Exception ex)
            {
                WriteError(ex.Message);
                return 1;
            }
        }

        static int Main(string[] args)
        {
            string configPath = null;
            string[] packagesDirectories = null;
            string frameworkVersion = null;
            bool showHelp = false, verifyOnly = false;

            var options = new OptionSet
            {
                { "v|verify", "Verify that the generated types are as expected, and return a non-zero exit code if they do not match. Nothing will be updated in this mode.", _ => verifyOnly = true },
                { "c|cfg=", "Specify a file that contains configuration", s => configPath = s },
                { "p|packages=", "Specify directory where NuGet packages are stored", s => packagesDirectories = s.Split(";") },
                { "f|framework=", "Specify the framework version (eg. v7.0)", s => frameworkVersion = s },
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

            var config = ReadConfig(configPath);
            if (config == null)
            {
                return 1;
            }

            if (packagesDirectories != null && packagesDirectories.Length > 0)
            {
                config.PackagesDirectories ??= new List<string>();
                foreach (var d in packagesDirectories)
                {
                    if (!config.PackagesDirectories.Contains(d))
                    {
                        config.PackagesDirectories.Add(d);
                    }
                }
            }

            if (frameworkVersion != null)
            {
                config.TargetFrameworkVersion = frameworkVersion;
            }

            var processor = new Processor(config);

            if (verifyOnly)
            {
                return MainTask(processor.VerifyTypesAsync());
            }
            else
            {
                return MainTask(processor.UpdateTypesAsync());
            }
        }

        private static Config ReadConfig(string configPath)
        {
            try
            {
                return Config.ReadFromFile(configPath);
            }
            catch (Exception ex)
            {
                WriteError($"Error reading config file {configPath}: {ex.Message}.");
                return null;
            }
        }
    }
}
