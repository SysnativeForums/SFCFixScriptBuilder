using System.Text;

namespace SFCFixScriptBuilder.Helpers
{
    public static class Formatter
    {
        public static string FormatRegBinary(string value, int end, int start = 63, int step = 75)
        {
            StringBuilder builder = new StringBuilder($"{value.Substring(0, 63)}\\\n");

            Range range = new Range(start, end);

            foreach (int i in range.Iterate(step))
            {
                if (i + step < end)
                {
                    builder.AppendLine($"{value.Substring(i, step)}\\");
                }
                else
                {
                    builder.AppendLine($@"{value.Substring(i)}");
                    break;
                }
            }

            return builder.ToString();
        }
    }
}
