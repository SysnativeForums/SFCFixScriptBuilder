namespace SFCFixScriptBuilder.Constants
{
    public static class Patterns
    {
        public static readonly string S256H_Mark = "(?=S256H).*";
        public static readonly string F_Mark = "(?=f\\!).*";
        public static readonly string F256_Mark = "(?=f256\\!).*";
        public static readonly string C_Mark = "(?=c\\!).*";
        public static readonly string V_Mark = "(?=v\\!).*";
        public static readonly string I_Mark = "(?>i\\!).*";
        public static readonly string Identity = "identity";
    }
}
