using System;
using System.Collections.Generic;
using System.Text;
using IqApiNetCore.Utilities;
using System.Runtime.InteropServices;

namespace IqApiNetCore
{
    //OutputLogger
    public class Logger
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        public Logger(bool showConsole)
        {
            if (showConsole)
                AllocConsole();
        }
        public void Log(object text)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + "| " + text.ToString());
        }
        public void Log(object text, ConsoleColor textColor)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = textColor;
            Console.WriteLine(DateTime.Now.ToLongTimeString() + "| " + text.ToString());
            Console.ForegroundColor = oldColor;
        }

        public void ShowResults(List<int> results)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.WriteLine("");
            Console.Write(DateTime.Now.ToLongTimeString() + "| ");
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] == 1)
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (results[i] == -1)
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[_]");
                Console.ForegroundColor = oldColor;
            }
            Console.WriteLine("");
        }
    }
}
