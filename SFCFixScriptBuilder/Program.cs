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
            bool siblings = false;

            Console.WriteLine("SFCFixScriptBuilder version 0.0.6--prerelease\n");

            if (arguments.Contains("--help"))
            {
                BuildHelpMenu();
                return;
            }

            if (arguments.Length < 4)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid number of arguments; you must provide a valid key name or log file along with the selected option");
                return;
            }
            else
            {
                string[] valid_options = new [] { "--packages", "--components", "--indexes", "--families", "--componentdetect", "--packagedetect", "--catalogs", "--deployments"};
                string[] selected_options = arguments.Intersect(valid_options).ToArray();

                if (selected_options.Length > 1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Please only select one option");
                    return;
                }

                hive = Array.IndexOf(arguments, "-hive") > -1 ? arguments[Array.IndexOf(arguments, "-hive") + 1] : @$"{GetEnvironmentVariable("systemroot")}\system32\config\COMPONENTS";
                log = Array.IndexOf(arguments, "-log") > -1 ? arguments[Array.IndexOf(arguments, "-log") + 1] : string.Empty;
                key = Array.IndexOf(arguments, "-key") > -1 ? arguments[Array.IndexOf(arguments, "-key") + 1] : string.Empty;
                version = Array.IndexOf(arguments, "-version") > -1 ? arguments[Array.IndexOf(arguments, "-version") + 1] : string.Empty;
                cbs = Array.IndexOf(arguments, "-cbs") > -1 ? arguments[Array.IndexOf(arguments, "-cbs") + 1] : string.Empty;
                option = selected_options.FirstOrDefault();

                if (arguments.Contains("--full") || !string.IsNullOrWhiteSpace(key))
                {
                    fullkey = true;
                }

                if (arguments.Contains("--sibilings"))
                {
                    siblings = true;
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
                case "--components":
                    LoadComponentsHive(hive, "SOURCE", ref builder);
                    await builder.BuildMissingComponentsAsync(fullkey, siblings);
                    break;
                case "--deployments":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadCBSHive(cbs, "CBS", ref builder);
                    LoadComponentsHive(hive, "SOURCE", ref builder);
                    await builder.BuildMissingDeploymentsAsync(fullkey, siblings);
                    break;
                case "--families":
                    LoadComponentsHive(hive, "SOURCE", ref builder);
                    await builder.BuildMissingComponentFamiliesAsync(fullkey);
                    break;
                case "--catalogs":
                    LoadComponentsHive(hive, "SOURCE", ref builder);
                    await builder.BuildMissingCatalogsAsync(fullkey);
                    break;
                case "--packages":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadCBSHive(cbs, "CBS", ref builder);
                    await builder.BuildMissingPackagesAsync(fullkey);
                    break;
                case "--indexes":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadCBSHive(cbs, "CBS", ref builder);
                    await builder.BuildMissingPackageIndexesAsync(fullkey);
                    break;
                case "--packagedetect":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadCBSHive(cbs, "CBS", ref builder);
                    await builder.BuildMissingPackageDetectAsync(fullkey);
                    break;
                case "--componentdetect":
                    if (!string.IsNullOrWhiteSpace(cbs)) LoadCBSHive(cbs, "CBS", ref builder);
                    await builder.BuildMissingComponentDetectAsync(fullkey);
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
        menu.AppendLine("Repair Key(s)/Value(s): SFCFixScriptBuilder -hive <Path to hive> -log <Path to log> <option>");
        menu.AppendLine("Repair Key: SFCFixScriptBuilder -hive <Path to hive> -key <Key Path or Key Name> <option>");

        menu.AppendLine("\n");

        menu.AppendLine("-key: The name or path of the key which you wish to repair. This is an optional parameter.\n");
        menu.AppendLine("-hive: The path to the COMPONENTS hive. This is a optional parameter.\n The COMPONENTS hive of the current system will be used if this parameter is not set.\n");
        menu.AppendLine("-cbs: The path to the CBS hive. This is an optional parameter.\n The CBS subkey of the current system will be used if this parameter is not set.\n");
        menu.AppendLine("-log: The path to the .txt file which contains the list of keys or key/values to repair. This is an optional parameter.\n");
        menu.AppendLine("-version: The VersionedIndex number which the component family belongs to. This is an optional parameter.\n");
        menu.AppendLine("--full: Determines if you wish to rebuild (the) entire key(s). This is an optional parameter. By default, only the specified values will be rebuilt. If -key is set, then --full is implied to be set as well.\n");
        
        menu.AppendLine("Available Build/Repair Options: \n");
        menu.AppendLine("--components - Build Missing Component Key(s)");
        menu.AppendLine("--deployments - Build Missing Deployment Key(s)");
        menu.AppendLine("--families - Build Missing Component Family Key(s)");
        menu.AppendLine("--catalogs - Build Missing Catalog Key(s)");
        menu.AppendLine("--packages - Build Missing Package Key(s)");
        menu.AppendLine("--indexes - Build Missing Package Index Key(s)");
        menu.AppendLine("--packagedetect - Build Missing Package Detect Key(s)");
        menu.AppendLine("--componentdetect - Build Missing Component Detect Key(s)");
        Console.WriteLine(menu.ToString());

        Console.WriteLine("Please press any key to exit...");
        Console.ReadKey();
    }

    private static int LoadHive(string path, string name)
    {
        HiveLoader.GrantPrivileges();
        int result = HiveLoader.LoadHive(path, name);
        HiveLoader.RevokePrivileges();

        return result;
    }

    private static void LoadComponentsHive(string path, string name, ref RegistryScriptBuilder builder)
    {
        //Attempt to load the hive, if COMPONENTS hive has already been loaded then this will return an error
        int result = LoadHive(path, name);

        if (result != 0) 
        {
            //Assume that COMPONENTS hive must have already been loaded
            return;
        }

        builder.SetComponentsHiveName(name);
    }

    private static void LoadCBSHive(string path, string name, ref RegistryScriptBuilder builder)
    {
        LoadHive(path, name);
        builder.SetComponentBasedServicingPath(name);
    }

    private static void UnloadHive(string name)
    {
        HiveLoader.GrantPrivileges();
        HiveLoader.UnloadHive(name);
        HiveLoader.RevokePrivileges();
    }
}