namespace SFCFixScriptBuilder.Extensions
{
    internal static class ArrayExtensions
    {
        public static T? GetElementByValue<T>(this T[] array, T value)
        {
            return Array.IndexOf(array, value) > -1 ? array[Array.IndexOf(array, value) + 1] : default;
        }
    }
}
