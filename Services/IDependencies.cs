using System.Collections.Generic;
using NuGet.ProjectModel;

namespace Package.Helper.Services
{
  public interface IDependencies
  {
    IEnumerable<DependencyGraphSpec> GetAllDependencies(IEnumerable<string> projPaths);
    void PrintDependencies(IEnumerable<string> projPaths);
    void PrintFlattenDependencies(IEnumerable<string> projPaths, bool printName, bool getDirectory, string basePath);
    void PrintFlattenedPublicPackages(IEnumerable<string> projPaths);
  }
}
