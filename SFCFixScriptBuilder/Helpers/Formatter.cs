namespace SFCFixScriptBuilder.Helpers
{
    public static class Formatter
    {
        public static IList<string> FormatRegBinary(string value, int end, int start = 63, int step = 75)
        {
            string first_slice = $"{value.Substring(0, 63)}\\\n";
            string second_slice = $"{value.Substring(63, 75)}\\\n";
            IList<string> slices = new List<string> { first_slice, second_slice };

            Range range = new Range(start, end);

            foreach (int i in range.Iterate(step))
            {
                if (i + step < end)
                {
                    slices.Add($"{value.Substring(i, step)}\\\n");
                }
                else
                {
                    slices.Add($@"{value.Substring(i)}");
                    break;
                }
            }

            return slices;
        }
    }
}
