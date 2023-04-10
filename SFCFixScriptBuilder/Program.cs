// See https://aka.ms/new-console-template for more information
using System.Text;
using SFCFixScriptBuilder.RegistryHiveLoader;
using SFCFixScriptBuilder.RegistryScriptBuilder;

//TODO: Build SFCFix registry scripts from a Process Monitor trace

/* 1. Run Processor Monitor trace and save the filtered trace to identify the missing keys
 * 2. Export the trace as a .csv file and then cleanup by deleting the columns which aren't Path
 * 3. Save the "cleaned" file as .txt
 * 4. Load the source COMPONENTS hive, preferably this would be a database of known good keys & values
 * 5. Specify the fix you wish to carry out and then run, this will build a SFCFixScript which can be run with SFCFix
 */

try
{
    string[] arguments = Environment.GetCommandLineArgs();
    string hive = string.Empty;
    string log = string.Empty;

    if (arguments.Length < 5)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Please provide the path of the COMPONENTS hive and the log file");
        Console.WriteLine("Please press any key to exit...");
        Console.ReadKey();
        return;
    }
    else
    {
        for (int i = 1; i < arguments.Length; i++)
        {
            //Argument prefix will always be odd i.e. 1 or 3
            if (i % 2 != 0)
            {
                if (arguments[i].StartsWith("-c"))
                {
                    hive = arguments[i + 1];
                }
                else
                {
                    log = arguments[i + 1];
                }
            }
        }
    }

    if (string.IsNullOrWhiteSpace(log) || string.IsNullOrWhiteSpace(hive)) Console.WriteLine("Please provide a valid hive and/or log path");

    Console.WriteLine("SFCFixScriptBuilder version 0.0.1 (prerelease)");

    StringBuilder menu = new StringBuilder();
    menu.AppendLine("Available Options: \n");
    menu.AppendLine("1. Build Missing Values for Component Key(s)");
    menu.AppendLine("2. Build Missing Values for Deployment Key(s)");
    menu.AppendLine("3. Build Missing Values for Component Family Key(s)");
    menu.AppendLine("4. Build Missing Component Key(s)");
    menu.AppendLine("5. Build Missing Deployment Key(s)");
    menu.AppendLine("6. Build Missing Component Family Key(s)");
    Console.WriteLine(menu.ToString());

    Console.Write("Please enter the number for the operation you wish to run: ");
    string option = Console.ReadLine();

    HiveLoader.GrantPrivileges();
    HiveLoader.LoadHive(hive, "SOURCE");
    HiveLoader.RevokePrivileges();

    RegistryScriptBuilder builder = new RegistryScriptBuilder(log);

    switch (option)
    {
        case "1":
            await builder.BuildMissingComponentValuesAsync();
            break;
        case "2":
            await builder.BuildMissingDeploymentValuesAsync();
            break;
        case "3":
            Console.Write("Please enter the versioned index: ");
            builder.Version = Console.ReadLine();
            await builder.BuildMissingComponentFamilyValuesAsync();
            break;
        case "4":
            await builder.BuildMissingComponentValuesAsync(true);
            break;
        case "5":
            await builder.BuildMissingDeploymentValuesAsync(true);
            break;
        case "6":
            Console.Write("Please enter the versioned index: ");
            builder.Version = Console.ReadLine();
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
    Console.WriteLine("Something went wrong! Please see exception details below.");
    Console.WriteLine(e.Message);
    Console.ResetColor();
}
finally
{
    HiveLoader.GrantPrivileges();
    HiveLoader.UnloadHive("SOURCE");
    HiveLoader.RevokePrivileges();

    Console.WriteLine("Please press any key to exit...");
    Console.ReadKey();
}

