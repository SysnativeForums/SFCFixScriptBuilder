namespace SFCFixScriptBuilder.Constants
{
    public static class Prefixes
    {
        public static readonly string ComponentsPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\DerivedData\Components\";
        public static readonly string ComponentFamiliesPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\DerivedData\VersionedIndex\{Version}\ComponentFamilies\";

        public static readonly string CatalogsPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\CanonicalData\Catalogs\";
        public static readonly string DeploymentsPrefix = @"[HKEY_LOCAL_MACHINE\COMPONENTS\CanonicalData\Deployments\";

        public static readonly string PackagesPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages\";
        public static readonly string PackageIndexPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\PackageIndex\";
        public static readonly string PackageDetectPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\PackageDetect\";
        public static readonly string ComponentDetectPrefix = @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\ComponentDetect\";

    }
}
