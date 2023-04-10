using System.Text;
using SFCFixScriptBuilder.RegistryHiveLoader;
using SFCFixScriptBuilder.RegistryScriptBuilder;

internal class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string hive = string.Empty;
            string log = string.Empty;
            string option = string.Empty;
            string key = string.Empty;
            string version = string.Empty;

            Console.WriteLine("SFCFixScriptBuilder version 0.0.2 (prerelease)\n");

            if (arguments.Contains("-help"))
            {
                BuildHelpMenu();
                return;
            }

            if (arguments.Length < 7)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid number of arguments; you must provide a valid hive path, key name or log file and the selected option");
                return;
            }
            else
            {
                hive = arguments[Array.IndexOf(arguments, "-hive") + 1];
                log = arguments[Array.IndexOf(arguments, "-log") + 1];
                option = arguments[Array.IndexOf(arguments, "-option") + 1];
                key = arguments[Array.IndexOf(arguments, "-key") + 1];
                version = arguments[Array.IndexOf(arguments, "-version") + 1];
            }

            if ((string.IsNullOrWhiteSpace(log) && string.IsNullOrWhiteSpace(key)) || string.IsNullOrWhiteSpace(hive))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid arguments; please check your argument values");
                return;
            }

            HiveLoader.GrantPrivileges();
            HiveLoader.LoadHive(hive, "SOURCE");
            HiveLoader.RevokePrivileges();

            RegistryScriptBuilder builder = new RegistryScriptBuilder(log, key);

            switch (option)
            {
                case "1":
                    await builder.BuildMissingComponentValuesAsync();
                    break;
                case "2":
                    await builder.BuildMissingDeploymentValuesAsync();
                    break;
                case "3":
                    builder.Version = version;
                    await builder.BuildMissingComponentFamilyValuesAsync();
                    break;
                case "4":
                    await builder.BuildMissingComponentValuesAsync(true);
                    break;
                case "5":
                    await builder.BuildMissingDeploymentValuesAsync(true);
                    break;
                case "6":
                    builder.Version = version;
                    await builder.BuildMissingComponentFamilyValuesAsync(true);
                    break;
                default:
                    break;
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
            HiveLoader.GrantPrivileges();
            HiveLoader.UnloadHive("SOURCE");
            HiveLoader.RevokePrivileges();

            Console.ReadKey();
        }
    }

    private static void BuildHelpMenu()
    {
        StringBuilder menu = new StringBuilder();

        menu.AppendLine("SFCFixScriptBuilder Help\n");
        menu.AppendLine("Repair Key(s)/Value(s): sfcfixscriptbuilder -hive <Path to hive> -log <Path to log> -option <option number>");
        menu.AppendLine("Repair Key: sfcfixscriptbuilder -hive <Path to hive> -key <Key Path or Key Name> -option <option number>");

        menu.AppendLine("\n");

        menu.AppendLine("-key: The name or path of the key which you wish to repair. This is an optional parameter.\n");
        menu.AppendLine("-hive: The path to the source hive, this may be a COMPONENTS or CBS hive. This is a mandatory parameter.\n");
        menu.AppendLine("-log: The path to the .txt file which contains the list of keys or key/values to repair. This is an optional parameter.\n");
        menu.AppendLine("-version: The VersionedIndex number which the component family belongs to. This is an optional parameter.\n");
        
        menu.AppendLine("-option: This is the repair operation you wish to carry out. This is a mandatory parameter.\n");
        menu.AppendLine("Available Options: \n");
        menu.AppendLine("1. Build Missing Values for Component Key(s)");
        menu.AppendLine("2. Build Missing Values for Deployment Key(s)");
        menu.AppendLine("3. Build Missing Values for Component Family Key(s)");
        menu.AppendLine("4. Build Missing Component Key(s)");
        menu.AppendLine("5. Build Missing Deployment Key(s)");
        menu.AppendLine("6. Build Missing Component Family Key(s)");
        Console.WriteLine(menu.ToString());
    }
}