using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.ProjectModel;

namespace Package.Helper.Services
{
  public class Dependencies : IDependencies
  {
    private readonly IDotNetRunner _dotNetRunner;

    public Dependencies(IDotNetRunner dotNetRunner)
    {
      _dotNetRunner = dotNetRunner;
    }

    private DependencyGraphSpec GetDependencies(string projectPath)
    {
      var tmpFile = new TemporaryFile(projectPath);
      var tmpFilePath = tmpFile.Path;

      string[] arguments =
      {
        "msbuild", $"\"{projectPath}\"", "/t:GenerateRestoreGraphFile",
        $"/p:RestoreGraphOutputPath=\"{tmpFilePath}\""
      };

      // Blocking run
      var runStatus = _dotNetRunner.Run(Path.GetDirectoryName(projectPath), arguments);

      if (!runStatus.IsSuccess)
      {
        throw new Exception(
          $"Unable to process the project `{projectPath}. Are you sure this is a valid .csproj/.sln?" +
          "\r\n\r\nHere is the full error message returned from the Microsoft Build Engine:\r\n\r\n" +
          runStatus.Output);
      }

      var dependencyGraphText = File.ReadAllText(tmpFilePath);
      tmpFile.Delete();
      return new DependencyGraphSpec(JsonConvert.DeserializeObject<JObject>(dependencyGraphText));
    }

    public IEnumerable<DependencyGraphSpec> GetAllDependencies(IEnumerable<string> projPaths)
    {
      var depTasks = projPaths
        .Aggregate(new List<Task<DependencyGraphSpec>>(), (list, projPath) =>
        {
          list.Add(Task<DependencyGraphSpec>.Factory.StartNew(() => GetDependencies(projPath)));

          return list;
        })
        .ToArray();

      Task.WaitAll(depTasks);

      return depTasks
        .Select(x => x.Result)
        .ToList();
    }

    public void PrintDependencies(IEnumerable<string> projPaths)
    {
      foreach (var projectPath in projPaths)
      {
        var dependencyDict = new Dictionary<string, PackageSpec>();

        var normalizedPath = projectPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        var dependencyGraph = GetDependencies(projectPath);

        foreach (var project in dependencyGraph.Projects)
        {
          if (!dependencyDict.ContainsKey(project.FilePath))
          {
            dependencyDict.Add(project.FilePath, project);
          }
        }

        LevelPrinter(dependencyDict, normalizedPath);
      }
    }

    public void PrintFlattenDependencies(IEnumerable<string> projPaths, bool printName, bool getDirectory, string basePath)
    {
      foreach (var projectPath in projPaths)
      {
        var list = new List<string>();

        var dependencyGraph = GetDependencies(projectPath);

        foreach (var project in dependencyGraph.Projects)
        {
          if (printName)
          {
            list.Add(project.Name);
          }
          else
          {
            var path = project.FilePath;
            if (getDirectory)
            {
              DirectoryInfo di = new DirectoryInfo(path);
              path = di.Parent.FullName;
            }
            if (basePath != "")
            {
              path = Path.GetRelativePath(basePath, path);
            }
            path = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            list.Add(path);
          }
        }

        list.Sort();

        foreach (var value in list)
        {
          Console.WriteLine(value);
        }
      }
    }

    public void PrintFlattenedPublicPackages(IEnumerable<string> projPaths)
    {
      foreach (var projectPath in projPaths)
      {
        var packageDict = new Dictionary<string, List<string>>();

        var dependencyGraph = GetDependencies(projectPath);

        foreach (var project in dependencyGraph.Projects)
        {
          foreach (var targetFramework in project.TargetFrameworks)
          {
            foreach (var dependency in targetFramework.Dependencies)
            {
              var packageName = dependency.Name;
              var packageVersion = dependency.LibraryRange.VersionRange.ToShortString();
              // Auto referenced package should not be included
              if (dependency.AutoReferenced)
              {
                continue;
              }

              if (!packageDict.ContainsKey(dependency.Name))
              {
                packageDict[packageName] = new List<string> {packageVersion};
              }
              else if (!packageDict[packageName].Contains(packageVersion))
              {
                packageDict[packageName].Add(packageVersion);
              }

              packageDict[packageName].Sort(Versioning.IsNewSemverLargerComparer);
            }
          }
        }

        PackageVersionPrinter(packageDict);
      }
    }

    private static void LevelPrinter(IReadOnlyDictionary<string, PackageSpec> data, string elementName,
      int indentLevel = 0)
    {
      if (elementName.EndsWith(".sln"))
      {
        Logger.Error("Unable to retrive root of hierarchy from .sln file");
        return;
      }
      else if (elementName == null || !data.ContainsKey(elementName))
      {
        throw new Exception("Unable to locate the root project.");
      }
      var name = data[elementName].Name;

      var projectPathList = data[elementName]
        .RestoreMetadata
        .TargetFrameworks
        .ToList()
        .FirstOrDefault()
        ?.ProjectReferences
        .Select(x => x.ProjectPath)
        .OrderBy(x => x)
        .ToList();
      var indent = "";
      for (var i = indentLevel; i > 0; i--)
      {
        indent = indent.Insert(0, i == indentLevel ? "├── " : "│   ");
      }

      Console.WriteLine($"{indent}{name}");
      if (projectPathList == null) return;
      foreach (var nextElementName in projectPathList)
      {
        LevelPrinter(data, nextElementName, indentLevel + 1);
      }
    }

    private static void PackageVersionPrinter(IReadOnlyDictionary<string, List<string>> data)
    {
      var sortedPackages = data.Keys.ToList();
      sortedPackages.Sort();
      var maxLength = sortedPackages.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur).Length;

      foreach (var packageName in sortedPackages)
      {
        Console.WriteLine($"{packageName.PadRight(maxLength)}     {string.Join(", ", data[packageName])}");
      }
    }
  }

  internal class TemporaryFile
  {
    public readonly string Identifier;
    public readonly string Path;

    public TemporaryFile(string identifier)
    {
      Identifier = identifier;
      Path = System.IO.Path.GetTempPath() + Guid.NewGuid() + ".dg";
    }

    public void Delete()
    {
      if (File.Exists(Path))
      {
        File.Delete(Path);
      }
    }
  }
}
