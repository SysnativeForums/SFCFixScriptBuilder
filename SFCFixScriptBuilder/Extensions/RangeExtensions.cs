namespace SFCFixScriptBuilder.Extensions
{
    public static class RangeExtensions
    {
        public static IEnumerable<int> Iterate(this Range range, int step = 1)
        {
            for (var i = range.Start.Value; i < range.End.Value; i += step)
            {
                yield return i;
            }
        }
    }
}
