namespace SFCFixScriptBuilder.Helpers
{
    public static class RangeExtensions
    {
        public static IEnumerable<int> Iterate(this Range range, int step = 1)
        {
            for (int i = range.Start.Value; i < range.End.Value;)
            {
                if (i + step < range.End.Value)
                {
                    yield return i += step;
                }
                else
                {
                    //Remebers the last position during the iteration
                    yield return i;
                }
            }
        }
    }
}
