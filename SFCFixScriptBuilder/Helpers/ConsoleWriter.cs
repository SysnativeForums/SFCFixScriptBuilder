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
