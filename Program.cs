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

    [Verb("dep", HelpText = "To get dependency graph of specific .csproj")]
    private class DependenciesOptions
    {
      [CommandLine.Option('p', "path", Default = "",
        HelpText = "Path of the .csproj file. Default to first .csproj found in current directory")]
      public string Path { get; set; }
      [CommandLine.Option("public", Default = false, HelpText = "Get all public packages required instead of the dependency graph")]
      public bool Public { get; set; }
    }

    [Verb("format", HelpText = "Format .csproj file")]
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

    private static void RunPrintDependencies(string path)
    {
      var dep = _container.Resolve<IDependencies>();
      var csprojPaths = Helpers.ResolveCsproj(path);

      dep.PrintDependencies(csprojPaths);
    }

    private static void RunPrintPublicPackages(string path)
    {
      var dep = _container.Resolve<IDependencies>();
      var csprojPaths = Helpers.ResolveCsproj(path);
            
      dep.PrintFlattenedPublicPackages(csprojPaths);
    }

    private static void RunFormat(string path)
    {
      var csprojPaths = Helpers.ResolveCsproj(path);

      Format.FormatAll(csprojPaths);
    }
  }
}
