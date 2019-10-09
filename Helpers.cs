using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Package.Helper
{
    public static class Helpers
    {
        public static IEnumerable<string> ResolveCsproj(string path)
        {
            // Constructor handling string
            if (string.IsNullOrEmpty(path))
            {
                // Go find the first csproj in the current working directory
                var files = Directory.GetFiles(Directory.GetCurrentDirectory());
                path = files.FirstOrDefault(file => Path.GetExtension(file) == ".csproj");
                if (string.IsNullOrEmpty(path))
                {
                    throw new Exception(
                        "Unable to find any .csproj file in your current directory. You may specific path(s) as arguments.");
                }

                return new List<string> {path};
            }

            var paths = path.Split('|').ToList();
            return paths.ResolvePaths();
        }

        public static IEnumerable<string> ResolveCsproj(IEnumerable<string> paths)
        {
            // Constructor handling list of strings
            return paths.ResolvePaths();
        }

        public static string GetDefaultPackageIdFromCsprojPath(string path)
        {
            var filename = Path.GetFileName(path);
            
            return Regex.Replace(filename, "\\.[Cc][Ss][Pp][Rr][Oo][Jj]$", "");
        }

        public static string GetAbsolutePathFromCsprojPathAndRelativeCsprojPath(string path, string relativePath)
        {
            relativePath = relativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var combinedPath = Path.Combine(Path.GetDirectoryName(path), relativePath);
            return Path.GetFullPath(combinedPath);
        }

        private static string ResolvePath(this string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            // Resolve the path to absolute
            path = Path.GetFullPath(new Uri(Path.GetFullPath(path)).LocalPath);
            return path;
        }

        private static IEnumerable<string> ResolvePaths(this IEnumerable<string> paths)
        {
            return paths
                .Select(path => path.ResolvePath())
                .Where(File.Exists)
                .ToList();
        }
        
        // Kotlin: fun <T, R> T.let(block: (T) -> R): R
        public static R Let<T, R>(this T self, Func<T, R> block)
        {
            return block(self);
        }

        // Kotlin: fun <T> T.also(block: (T) -> Unit): T
        public static T Also<T>(this T self, Action<T> block)
        {
            block(self);
            return self;
        }
    }
}