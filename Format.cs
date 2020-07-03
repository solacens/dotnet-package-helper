using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Package.Helper
{
  public static class Format
  {
    public static void FormatAll(IEnumerable<string> paths)
    {
      foreach (var path in paths)
      {
        FormatByPath(path);
      }
    }

    private static void FormatByPath(string path)
    {
      var projContent = File.ReadAllText(path);

      var doc = XDocument.Parse(projContent);

      if (doc.Root == null)
      {
        Logger.Error($"[{path}]: Invalid XML, Missing XML Root");
        return;
      }

      var propertyGroup = doc.Root.Element("PropertyGroup");

      if (propertyGroup == null)
      {
        Logger.Error($"[{path}]: Invalid XML, Missing <PropertyGroup>");
        return;
      }

      var targetFramework = "";
      var reservedProperties = new Dictionary<string, string>
      {
        {"PackageId", Helpers.GetDefaultPackageIdFromCsprojPath(path)},
        {"VersionPrefix", "1.0.0"},
        {"Description", ""},
        {"PackageTags", ""},
        {"IsPackable", "true"},
        {"Authors", ""},
        {"PackageRequireLicenseAcceptance", "false"},
        {"PackageReleaseNotes", ""},
        {"Copyright", "Copyright 2019 (c). All rights reserved."}
      };
      var otherProperties = new Dictionary<string, string>();

      foreach (var node in propertyGroup.Elements())
      {
        var nodeTag = node.Name.ToString();

        if (nodeTag == "TargetFramework")
        {
          targetFramework = node.Value;
          if (!node.Value.StartsWith("netstandard"))
          {
            Logger.Warn($"[{path}]: Target Framework is not dotnet standard. Current value: [{node.Value}]");
          }
        }
        else if (reservedProperties.ContainsKey(nodeTag))
        {
          reservedProperties[nodeTag] = node.Value;
        }
        else if (nodeTag.StartsWith("GenerateAssembly") || nodeTag == "Configurations")
        {
        }
        else
        {
          otherProperties[nodeTag] = node.Value;
        }
      }

      propertyGroup.RemoveNodes();

      propertyGroup.Add(new XComment(" Start of target framework(s) "));
      propertyGroup.Add(new XElement("TargetFramework", targetFramework));
      propertyGroup.Add(new XComment(" End of target framework(s) "));

      propertyGroup.Add(new XComment(" Start of metadata for 'dotnet pack' "));
      foreach (var (key, value) in reservedProperties)
      {
        propertyGroup.Add(new XElement(key, value));
      }

      propertyGroup.Add(new XComment(" End of metadata for 'dotnet pack' "));

      propertyGroup.Add(new XComment(" Start of assembly attribute settings "));
      propertyGroup.Add(new XElement("GenerateAssemblyTitleAttribute", "false"));
      propertyGroup.Add(new XElement("GenerateAssemblyProductAttribute", "false"));
      propertyGroup.Add(new XElement("GenerateAssemblyCopyrightAttribute", "false"));
      propertyGroup.Add(new XElement("GenerateAssemblyVersionAttribute", "false"));
      propertyGroup.Add(new XElement("GenerateAssemblyFileVersionAttribute", "false"));
      propertyGroup.Add(new XElement("GenerateAssemblyInfo", "false"));
      propertyGroup.Add(new XComment(" End of assembly attribute settings "));

      propertyGroup.Add(new XComment(" Start of other attributes "));
      foreach (var (key, value) in otherProperties)
      {
        propertyGroup.Add(new XElement(key, value));
      }

      propertyGroup.Add(new XComment(" End of other attributes "));

      var xmlString = doc.ToString();

      // Some XML String formatting

      xmlString = Regex.Replace(xmlString, "^  <([^/])", "\n  <$1", RegexOptions.Multiline);
      xmlString = Regex.Replace(xmlString, "^</Project>$", "\n</Project>", RegexOptions.Multiline);
      // Adding trailing zero for others habits
      xmlString = Regex.Replace(xmlString, "<(PackageReference|ProjectReference|Reference|None) (.*)\"/>$", "<$1 $2\" />", RegexOptions.Multiline);
      // Expanding back with no value
      xmlString = Regex.Replace(xmlString, "<([a-zA-Z0-9]+)/>", "<$1></$1>", RegexOptions.Multiline);

      File.WriteAllText(path, xmlString);
    }
  }
}
