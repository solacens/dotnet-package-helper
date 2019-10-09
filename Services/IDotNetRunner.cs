// Credit for: https://github.com/jerriep/dotnet-outdated/blob/master/src/DotNetOutdated.Core/Services/IDotNetRunner.cs

namespace Package.Helper.Services
{
  public interface IDotNetRunner
  {
    RunStatus Run(string workingDirectory, string[] arguments);
  }
}
