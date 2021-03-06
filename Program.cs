using System;
using System.Collections.Generic;
using CommandLine;
using McMaster.Extensions.CommandLineUtils;
using Package.Helper.Services;
using Unity;

namespace Package.Helper
{
  public static class Program
  {
    private static IUnityContainer _container;

    [Verb("dep", HelpText = "To get dependency graph of specific .csproj/.sln")]
    private class DependenciesOptions
    {
      [CommandLine.Option('p', "path", Default = "",
        HelpText = "Path of the .csproj/.sln file. Default to first .csproj/.sln found in current directory")]
      public string Path { get; set; }
      [CommandLine.Option("public", Default = false, HelpText = "Get all public packages required instead of the dependency graph")]
      public bool Public { get; set; }
      [CommandLine.Option("flatten", Default = false, HelpText = "Get flatten list of packages")]
      public bool Flatten { get; set; }
      [CommandLine.Option("print-name", Default = false, HelpText = "[Only capable when flattened] Print name instead of paths")]
      public bool PrintName { get; set; }
      [CommandLine.Option("get-directory", Default = true, HelpText = "[Only capable when flattened] Print paths' parent directory instead")]
      public bool GetDirectory { get; set; }
      [CommandLine.Option("base-path", Default = "", HelpText = "[Only capable when flattened] Print paths base on the provided path")]
      public string BasePath { get; set; }
    }

    [Verb("format", HelpText = "Format .csproj file (.sln not included)")]
    private class FormatOptions
    {
      [CommandLine.Option('p', "path", Default = "",
        HelpText = "Path of the .csproj file. Default to first .csproj found in current directory")]
      public string Path { get; set; }
    }

    private static void NotParsedFunc(IEnumerable<Error> arg)
    {
      // Do nothing
    }

    public static void Main(string[] args)
    {
      _container = new UnityContainer();

      _container.RegisterType<IDependencies, Dependencies>();
      _container.RegisterType<IDotNetRunner, DotNetRunner>();

      Parser.Default.ParseArguments<DependenciesOptions, FormatOptions>(args)
        .WithParsed<DependenciesOptions>(options =>
        {
          if (options.Public)
          {
            RunPrintPublicPackages(
              path: options.Path
            );
          }
          else if(options.Flatten){
            RunPrintDependencies(
              path: options.Path,
              flatten: true,
              printName: options.PrintName,
              getDirectory: options.GetDirectory,
              basePath: options.BasePath
            );
          }
          else
          {
            RunPrintDependencies(
              path: options.Path
            );
          }
        })
        .WithParsed<FormatOptions>(options =>
          RunFormat(
            path: options.Path
          ))
        .WithNotParsed(NotParsedFunc);

      Console.ResetColor();
    }

    private static void RunPrintDependencies(string path, bool flatten = false, bool printName = false, bool getDirectory = false, string basePath = "")
    {
      var dep = _container.Resolve<IDependencies>();
      var projPaths = Helpers.ResolveProj(path);

      if (!flatten)
      {
        dep.PrintDependencies(projPaths);
      }
      else
      {
        dep.PrintFlattenDependencies(projPaths, printName, getDirectory, basePath);
      }
    }

    private static void RunPrintPublicPackages(string path)
    {
      var dep = _container.Resolve<IDependencies>();
      var projPaths = Helpers.ResolveProj(path);
            
      dep.PrintFlattenedPublicPackages(projPaths);
    }

    private static void RunFormat(string path)
    {
      var projPaths = Helpers.ResolveProj(path);

      Format.FormatAll(projPaths);
    }
  }
}
