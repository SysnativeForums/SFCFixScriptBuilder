using System.Runtime.CompilerServices;
using System.Text;
using SFCFixScriptBuilder.RegistryHiveLoader;
using SFCFixScriptBuilder.RegistryScriptBuilder;
using static System.Environment;

internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            string[] arguments = GetCommandLineArgs();
            string hive = string.Empty;
            string log = string.Empty;
            string option = string.Empty;
            string key = string.Empty;
            string version = string.Empty;
            string cbs = string.Empty;
            bool fullkey = false;

            Console.WriteLine("SFCFixScriptBuilder version 0.0.4--prerelease\n");

            if (arguments.Contains("-help"))
            {
                BuildHelpMenu();
                return;
            }

            if (arguments.Length < 5)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid number of arguments; you must provide a valid key name or log file along with the selected option");
                return;
            }
            else
            {
                hive = Array.IndexOf(arguments, "-hive") > -1 ? arguments[Array.IndexOf(arguments, "-hive") + 1] : @$"{GetEnvironmentVariable("systemroot")}\system32\config\COMPONENTS";
                log = Array.IndexOf(arguments, "-log") > -1 ? arguments[Array.IndexOf(arguments, "-log") + 1] : string.Empty;
                key = Array.IndexOf(arguments, "-key") > -1 ? arguments[Array.IndexOf(arguments, "-key") + 1] : string.Empty;
                version = Array.IndexOf(arguments, "-version") > -1 ? arguments[Array.IndexOf(arguments, "-version") + 1] : string.Empty;
                cbs = Array.IndexOf(arguments, "-cbs") > -1 ? arguments[Array.IndexOf(arguments, "-cbs") + 1] : string.Empty;
                option = Array.IndexOf(arguments, "-option") > -1 ? arguments[Array.IndexOf(arguments, "-option") + 1] : string.Empty;

                if (arguments.Contains("-full") || !string.IsNullOrWhiteSpace(key))
                {
                    fullkey = true;
                }
            }

            if (string.IsNullOrWhiteSpace(log) && string.IsNullOrWhiteSpace(key))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid arguments; please check your argument values");
                return;
            }

            RegistryScriptBuilder builder = new RegistryScriptBuilder(log, key, version);

            switch (option)
            {
                case "1":
                    LoadHive(hive, "SOURCE");
                    await builder.BuildMissingComponentValuesAsync(fullkey);
                    break;
                case "2":
                    LoadHive(hive, "SOURCE");
                    await builder.BuildMissingDeploymentValuesAsync(fullkey);
                    break;
                case "3":
                    LoadHive(hive, "SOURCE");
                    await builder.BuildMissingComponentFamilyValuesAsync(fullkey);
                    break;
                case "4":
                    LoadHive(hive, "SOURCE");
                    await builder.BuildMissingCatalogsAsync(fullkey);
                    break;
                case "5":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadHive(cbs, "CBS");
                    await builder.BuildMissingPackagesAsync(cbs, fullkey);
                    break;
                case "6":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadHive(cbs, "CBS");
                    await builder.BuildMissingPackageIndexesAsync(cbs, fullkey);
                    break;
                case "7":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadHive(cbs, "CBS");
                    await builder.BuildMissingPackageDetectAsync(cbs, fullkey);
                    break;
                case "8":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadHive(cbs, "CBS");
                    await builder.BuildMissingComponentDetectAsync(cbs, fullkey);
                    break;
                default:
                    Console.WriteLine("Please provide a valid option");
                    return;
            }

            Console.WriteLine("SFCFixScript.txt has been succesfully written to %userprofile%\\Desktop \n");
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Something went wrong! Please see exception details below.\n");
            Console.WriteLine(e.Message);
            Console.ResetColor();
        }
        finally
        {
            UnloadHive("SOURCE");
            UnloadHive("CBS");
        }
    }

    private static void BuildHelpMenu()
    {
        StringBuilder menu = new StringBuilder();

        menu.AppendLine("SFCFixScriptBuilder Help\n");
        menu.AppendLine("Repair Key(s)/Value(s): SFCFixScriptBuilder -hive <Path to hive> -log <Path to log> -option <option number>");
        menu.AppendLine("Repair Key: SFCFixScriptBuilder -hive <Path to hive> -key <Key Path or Key Name> -option <option number>");

        menu.AppendLine("\n");

        menu.AppendLine("-key: The name or path of the key which you wish to repair. This is an optional parameter.\n");
        menu.AppendLine("-hive: The path to the COMPONENTS hive. This is a optional parameter.\n The COMPONENTS hive of the current system will be used if this parameter is not set.\n");
        menu.AppendLine("-cbs: The path to the CBS hive. This is an optional parameter.\n The CBS subkey of the current system will be used if this parameter is not set.\n");
        menu.AppendLine("-log: The path to the .txt file which contains the list of keys or key/values to repair. This is an optional parameter.\n");
        menu.AppendLine("-version: The VersionedIndex number which the component family belongs to. This is an optional parameter.\n");
        menu.AppendLine("-full: Determines if you wish to rebuild (the) entire key(s). This is an optional parameter. By default, only the specified values will be rebuilt. If -key is set, then -full is implied to be set as well.\n");
        
        menu.AppendLine("-option: This is the repair operation you wish to carry out. This is a mandatory parameter.\n");
        menu.AppendLine("Available Options: \n");
        menu.AppendLine("1. Build Missing Component Key(s)");
        menu.AppendLine("2. Build Missing Deployment Key(s)");
        menu.AppendLine("3. Build Missing Component Family Key(s)");
        menu.AppendLine("4. Build Missing Catalog Key(s)");
        menu.AppendLine("5. Build Missing Package Key(s)");
        menu.AppendLine("6. Build Missing Package Index Key(s)");
        menu.AppendLine("7. Build Missing Package Detect Key(s)");
        menu.AppendLine("8. Build Missing Component Detect Key(s)");
        Console.WriteLine(menu.ToString());
    }

    private static void LoadHive(string path, string name)
    {
        HiveLoader.GrantPrivileges();
        HiveLoader.LoadHive(path, name);
        HiveLoader.RevokePrivileges();
    }

    private static void UnloadHive(string name)
    {
        HiveLoader.GrantPrivileges();
        HiveLoader.UnloadHive(name);
        HiveLoader.RevokePrivileges();
    }
}