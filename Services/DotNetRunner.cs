// Credit for: https://github.com/jerriep/dotnet-outdated/blob/master/src/DotNetOutdated.Core/Services/DotNetRunner.cs

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Package.Helper.Services
{
  public class RunStatus
  {
    public string Output { get; }
    public string Errors { get; }
    public int ExitCode { get; }

    public bool IsSuccess => ExitCode == 0;

    public RunStatus(string output, string errors, int exitCode)
    {
      Output = output;
      Errors = errors;
      ExitCode = exitCode;
    }
  }

  public class DotNetRunner : IDotNetRunner
  {
    public RunStatus Run(string workingDirectory, string[] arguments)
    {
      var psi = new ProcessStartInfo(DotNetExe.FullPathOrDefault(), string.Join(" ", arguments))
      {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      var p = new Process();
      try
      {
        p.StartInfo = psi;
        p.Start();

        var output = new StringBuilder();
        var errors = new StringBuilder();
        var outputTask = ConsumeStreamReaderAsync(p.StandardOutput, output);
        var errorTask = ConsumeStreamReaderAsync(p.StandardError, errors);

        var processExited = p.WaitForExit(20000);

        if (processExited == false)
        {
          p.Kill();

          return new RunStatus(output.ToString(), errors.ToString(), exitCode: -1);
        }

        Task.WaitAll(outputTask, errorTask);

        return new RunStatus(output.ToString(), errors.ToString(), p.ExitCode);
      }
      finally
      {
        p.Dispose();
      }
    }

    private static async Task ConsumeStreamReaderAsync(StreamReader reader, StringBuilder lines)
    {
      await Task.Yield();

      string line;
      while ((line = await reader.ReadLineAsync()) != null)
      {
        lines.AppendLine(line);
      }
    }
  }
}
