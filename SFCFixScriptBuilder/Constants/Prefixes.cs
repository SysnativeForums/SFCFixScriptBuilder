namespace SFCFixScriptBuilder.Constants
{
    public static class Prefixes
    {
        public const string ComponentsPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\DerivedData\Components\";
        public const string ComponentFamiliesPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\DerivedData\VersionedIndex\{Version}\ComponentFamilies\";

        public const string CatalogsPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\CanonicalData\Catalogs\";
        public const string DeploymentsPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\CanonicalData\Deployments\";

        public const string PackagesPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages\";
        public const string PackageIndexPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\PackageIndex\";
        public const string PackageDetectPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\PackageDetect\";
        public const string ComponentDetectPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\ComponentDetect\";

    }
}
