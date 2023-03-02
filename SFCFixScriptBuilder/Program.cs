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

string[] arguments = Environment.GetCommandLineArgs();
string hive = string.Empty;
string log = string.Empty;

if (arguments.Length < 5)
{
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

StringBuilder menu = new StringBuilder();
menu.AppendLine("Available Options: \n");
menu.AppendLine("1. Build Missing S256H Marks");
menu.AppendLine("2. Build Missing f! Marks");
Console.WriteLine(menu.ToString());

Console.Write("Please enter the operation you wish to run: ");
string option = Console.ReadLine();

HiveLoader.GrantPrivileges();
HiveLoader.LoadHive(hive, "SOURCE");
HiveLoader.RevokePrivileges();

RegistryScriptBuilder builder = new RegistryScriptBuilder(log);

switch (option)
{
    case "1":
        //Run S256H builder
        await builder.BuildMissingS256HMarksScriptAsync();
        break;
    case "2":
        //Run f! builder
        await builder.BuildMissingFMarksScriptAsync();
        break;
    default:
        break;
}

Console.WriteLine("The log file has been succesfully written");

HiveLoader.GrantPrivileges();
HiveLoader.UnloadHive("SOURCE");
HiveLoader.RevokePrivileges();

Console.ReadKey();