using System;

namespace Package.Helper
{
    public static class Logger
    {
        public static void Info(string str)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[Info]    {str}");
        }

        public static void Warn(string str)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Warn]    {str}");
        }

        public static void Error(string str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error]   {str}");
        }

        public static void Debug(string str)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[Debug]   {str}");
        }
    }
}