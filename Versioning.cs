using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Package.Helper
{
  public static class Versioning
  {
    private static (int, int, int)? ParseVersion(string semver)
    {
      var regex = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(-(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(\.(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)?(\+[0-9a-zA-Z-]+(\.[0-9a-zA-Z-]+)*)?$");
      var match = regex.Match(semver);
      if (!match.Success)
      {
        return null;
      }

      return (int.Parse(match.Groups["major"].Value), int.Parse(match.Groups["minor"].Value), int.Parse(match.Groups["patch"].Value));
    }

    private static string BumpVersion(string semver, Bumping which)
    {
      var parsed = ParseVersion(semver);
      if (parsed == null)
      {
        Logger.Warn($"[{semver}] is not a valid Semantic Versioning.");
        return semver;
      }

      var (major, minor, patch) = parsed.GetValueOrDefault();
      switch (which)
      {
        case Bumping.Major:
          major += 1;
          break;
        case Bumping.Minor:
          minor += 1;
          break;
        case Bumping.Patch:
          patch += 1;
          break;
      }

      return $"{major}.{minor}.{patch}";
    }

    public static bool IsNewSemverLarger(string oldSemver, string newSemver)
    {
      if (ParseVersion(oldSemver) == null || ParseVersion(newSemver) == null)
      {
        return false;
      }

      var (oldMajor, oldMinor, oldPatch) = ParseVersion(oldSemver).GetValueOrDefault();
      var (newMajor, newMinor, newPatch) = ParseVersion(newSemver).GetValueOrDefault();

      return newMajor >= oldMajor &&
             newMinor >= oldMinor &&
             newPatch >= oldPatch &&
             (newMajor > oldMajor || newMinor > oldMinor || newPatch > oldPatch);
    }

    public static int IsNewSemverLargerComparer(string oldSemver, string newSemver)
    {
      return IsNewSemverLarger(oldSemver, newSemver) ? -1 : 1;
    }

    private static string RetrieveVersionFromProjectFile(string path)
    {
      var csprojContent = File.ReadAllText(path);
      var regex = new Regex("<VersionPrefix>(?<version>[0-9\\.]+)</VersionPrefix>");

      var match = regex.Match(csprojContent);

      return match.Success ? match.Groups["version"].Value : "";
    }

    public static bool BumpVersionForCsproj(string path, string currentSemver, Bumping which = Bumping.Patch)
    {
      var csprojContent = File.ReadAllText(path);
      var nextSemver = BumpVersion(currentSemver, which);

      var newCsprojContent = Regex.Replace(csprojContent, "<VersionPrefix>[0-9\\.]+</VersionPrefix>", $"<VersionPrefix>{nextSemver}</VersionPrefix>");

      if (csprojContent == newCsprojContent)
      {
        return false;
      }

      File.WriteAllText(path, newCsprojContent);
      return true;
    }

    public static bool IsVersionBumped(string path, string currentSemver)
    {
      var retrievedVersion = RetrieveVersionFromProjectFile(path);

      return IsNewSemverLarger(currentSemver, retrievedVersion);
    }

    public enum Bumping
    {
      Major,
      Minor,
      Patch
    }
  }
}
