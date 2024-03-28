using SFCFixScriptBuilder.Constants;

namespace SFCFixScriptBuilder
{
    internal record KeyInformation
    {
        public SiblingKeyType SiblingKeyType { get; init; } = SiblingKeyType.None;

        public string Prefix { get; init; } = string.Empty;
    }
}
