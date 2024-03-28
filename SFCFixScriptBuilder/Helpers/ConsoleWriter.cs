using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFCFixScriptBuilder.Helpers
{
    internal static class ConsoleWriter
    {
        public static void WriteMessage(string message, ConsoleColor colour) 
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
