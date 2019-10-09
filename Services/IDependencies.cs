using System.Collections.Generic;
using NuGet.ProjectModel;

namespace Package.Helper.Services
{
    public interface IDependencies
    {
        IEnumerable<DependencyGraphSpec> GetAllDependencies(IEnumerable<string> csprojPaths);
        void PrintDependencies(IEnumerable<string> csprojPaths);
        void PrintFlattenedPublicPackages(IEnumerable<string> csprojPaths);
    }
}