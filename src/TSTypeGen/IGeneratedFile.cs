using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace TSTypeGen
{
  public interface IGeneratedFile
  {
    string FilePath { get; }
    Task<string> GetContentAsync(TypeBuilderConfig typeBuilderConfig, Config config, GeneratorContext generatorContext);
    Task<bool> VerifyAsync(TypeBuilderConfig typeBuilderConfig, Config config, GeneratorContext generatorContext);
    Task ApplyAsync(TypeBuilderConfig typeBuilderConfig, Config config, GeneratorContext generatorContext);
  }
}
