﻿using System.Text;

namespace SFCFixScriptBuilder.Helpers
{
    public static class Formatter
    {
        public static string FormatRegBinary(string value, int end, int start = 63, int step = 75)
        {
            var builder = new StringBuilder($"{value.Substring(0, 63)}\\\n");

            var range = new Range(start, end);

            foreach (var i in range.Iterate(step))
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
