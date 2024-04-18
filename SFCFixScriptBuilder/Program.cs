using Registry;
using SFCFixScriptBuilder.Extensions;
using SFCFixScriptBuilder.Helpers;
using SFCFixScriptBuilder.RegistryScriptBuilder;
using System.Text;
using static System.Environment;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var validOptions = new string[] { "-components", "-cbs", "-log", "-key", "--siblings" };
        var siblings = false;

        try
        {
            Console.WriteLine("SFCFixScriptBuilder version 0.0.9 --prerelease\n");

            // Check which options have been selected and ignore any invalid options
            var selectedOptions = args.Intersect(validOptions).ToArray();

            if (selectedOptions.Length <= 0)
            {
                ConsoleWriter.WriteMessage("Warning: Please ensure that you have provided at least one argument", ConsoleColor.Yellow);
                return;
            }

            var componentsPath = args.GetElementByValue("-components") ?? @$"{GetEnvironmentVariable("systemroot")}\system32\config\COMPONENTS";
            var log = args.GetElementByValue("-log") ?? string.Empty;
            var key = args.GetElementByValue("-key") ?? string.Empty;
            var cbsPath = args.GetElementByValue("-cbs") ?? string.Empty;

            if (args.Contains("--siblings"))
            {
                siblings = true;
            }

            if (string.IsNullOrWhiteSpace(log) && string.IsNullOrWhiteSpace(key))
            {
                ConsoleWriter.WriteMessage("Warning: The -key or -log argument is missing.", ConsoleColor.Yellow);
                return;
            }

            var builder = BuildRegistryScriptBuilder(componentsPath, cbsPath);

            // The user has provided a .txt log with the -log argument
            if (!string.IsNullOrWhiteSpace(log) && string.IsNullOrWhiteSpace(key))
            {
                if (File.Exists(log))
                {
                    var keys = await File.ReadAllLinesAsync(log);
                    var stringBuilder = new StringBuilder("::\n");

                    foreach (var keyPath in keys)
                    {
                        var partialScript = builder.BuildRegistryKeyScript(keyPath?.Trim()!, siblings, true);
                        stringBuilder.Append(partialScript);
                    }

                    await WriteSFCFixScriptAsync(stringBuilder.ToString());
                }
                else
                {
                    ConsoleWriter.WriteMessage("Error: The provided log file does not exist.", ConsoleColor.Red);
                    return;
                }
            }
            else
            {
                var keyScript = builder.BuildRegistryKeyScript(key, siblings, false);
                await WriteSFCFixScriptAsync(keyScript);
            }

            ConsoleWriter.WriteMessage(@"SFCFixScript.txt has been successfully written to %userprofile%\Desktop", ConsoleColor.Green);
        }
        catch (Exception e)
        {
            ConsoleWriter.WriteMessage($"Error: {e.Message}", ConsoleColor.Red);
        }
    }

    private static RegistryScriptBuilder BuildRegistryScriptBuilder(string componentsPath, string cbsPath)
    {
        var componentsHive = new RegistryHiveOnDemand(componentsPath);

        if (!string.IsNullOrWhiteSpace(cbsPath)) 
        {
            var cbsHive = new RegistryHiveOnDemand(cbsPath);
            return new RegistryScriptBuilder(ref componentsHive, ref cbsHive);
        }

        return new RegistryScriptBuilder(ref componentsHive);
    }

    private static async Task WriteSFCFixScriptAsync(string keyScript)
    {
        var _desktop = $@"{GetEnvironmentVariable("userprofile")}\Desktop";
        var path = @$"{_desktop}\SFCFixScript.txt";

        if (File.Exists(path))
        {
            var fileCount = Directory.EnumerateFiles(_desktop)
                .Where(f => f.Contains("SFCFixScript"))
                .Count();

            ConsoleWriter.WriteMessage("Warning: An existing SFCFixScript.txt file was found, do you wish to overwrite it [y/n]: ", ConsoleColor.Yellow);
            var answer = Console.ReadLine() ?? "n";

            if (answer.Equals("n", StringComparison.InvariantCultureIgnoreCase))
            {
                await File.WriteAllTextAsync(@$"{_desktop}\SFCFixScript ({fileCount + 1}).txt", keyScript);
                return;
            }
        }

        await File.WriteAllTextAsync(path, keyScript);
    }
}