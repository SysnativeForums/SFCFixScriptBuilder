using Microsoft.Win32;
using SFCFixScriptBuilder.Constants;
using SFCFixScriptBuilder.RegistryHiveLoader;
using System.Text;
using static System.Environment;

namespace SFCFixScriptBuilder.RegistryScriptBuilder
{
    public class RegistryScriptBuilder
    {
        private RegistryKey HKLM = HiveLoader.HKLM;
        private const string COMPONENTS = "SOURCE";
        private const string CBS = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing";
        private string Desktop = $@"{GetEnvironmentVariable("userprofile")}\Desktop";
        
        private string SourcePath { get; set; }
        private string KeyName { get; set; }
        private string Version { get; set; }

        public RegistryScriptBuilder(string sourcePath, string key, string version) 
        {
            SourcePath = sourcePath;
            KeyName = key;
            Version = version;
        }

        public async Task BuildMissingCatalogsAsync(bool buildkey = false)
        {
            RegistryKey catalogs = HKLM.OpenSubKey(@$"{COMPONENTS}\CanonicalData\Catalogs");

            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(catalogs, Prefixes.CatalogsPrefix, buildkey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(catalogs, Prefixes.CatalogsPrefix, buildkey);
            }

            CloseKeys(catalogs);
        }

        public async Task BuildMissingComponentValuesAsync(bool buildkey = false)
        {
            RegistryKey components = HKLM.OpenSubKey(@$"{COMPONENTS}\DerivedData\Components");
            
            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(components, Prefixes.ComponentsPrefix, buildkey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(components, Prefixes.ComponentsPrefix, buildkey);
            }

            CloseKeys(components);
        }

        public async Task BuildMissingDeploymentValuesAsync(bool buildkey = false)
        {
            RegistryKey deployments = HKLM.OpenSubKey($@"{COMPONENTS}\CanonicalData\Deployments");
            
            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(deployments, Prefixes.DeploymentsPrefix, buildkey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(deployments, Prefixes.DeploymentsPrefix, buildkey);
            }

            CloseKeys(deployments);
        }

        public async Task BuildMissingComponentFamilyValuesAsync(bool buildKey = false)
        {
            string prefix = Prefixes.ComponentFamiliesPrefix.Replace("{Version}", Version);
            RegistryKey component_families = HKLM.OpenSubKey(@$"{COMPONENTS}\DerivedData\VersionedIndex\{Version}\ComponentFamilies");
            
            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(component_families, prefix, buildKey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(component_families, prefix, buildKey);
            }
            
            CloseKeys(component_families);
        }

        public async Task BuildMissingPackagesAsync(string hive = "", bool buildKey = false)
        {
            RegistryKey packages = !string.IsNullOrWhiteSpace(hive) ? HKLM.OpenSubKey($@"{hive}\Packages") : HKLM.OpenSubKey($@"{CBS}\Packages");

            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(packages, Prefixes.PackagesPrefix, buildKey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(packages, Prefixes.PackagesPrefix, buildKey);
            }

            CloseKeys(packages);
        }

        public async Task BuildMissingPackageIndexesAsync(string hive = "", bool buildKey = false)
        {
            RegistryKey indexes = !string.IsNullOrWhiteSpace(hive) ? HKLM.OpenSubKey($@"{hive}\PackageIndex") : HKLM.OpenSubKey($@"{CBS}\PackageIndex");

            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(indexes, Prefixes.PackageIndexPrefix, buildKey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(indexes, Prefixes.PackageIndexPrefix, buildKey);
            }

            CloseKeys(indexes);
        }
        
        public async Task BuildMissingPackageDetectAsync(string hive = "", bool buildKey = false)
        {
            RegistryKey packageDetect = !string.IsNullOrWhiteSpace(hive) ? HKLM.OpenSubKey($@"{hive}\PackageDetect") : HKLM.OpenSubKey($@"{CBS}\PackageDetect");

            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(packageDetect, Prefixes.PackageDetectPrefix, buildKey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(packageDetect, Prefixes.PackageDetectPrefix, buildKey);
            }

            CloseKeys(packageDetect);
        }

        public async Task BuildMissingComponentDetectAsync(string hive = "", bool buildKey = false)
        {
            RegistryKey componentDetect = !string.IsNullOrWhiteSpace(hive) ? HKLM.OpenSubKey($@"{hive}\ComponentDetect") : HKLM.OpenSubKey($@"{CBS}\ComponentDetect");

            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(componentDetect, Prefixes.ComponentDetectPrefix, buildKey);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(componentDetect, Prefixes.ComponentDetectPrefix, buildKey);
            }

            CloseKeys(componentDetect);
        }

        #region Helpers

        private void CloseKeys(RegistryKey keys)
        {
            //Close any handles to keys otherwise the hive will be unable to be unloaded
            keys.Close();
            HKLM.Close();
        }

        private async Task WriteSFCFixScriptAsync(StringBuilder builder)
        {
            string lines = builder.ToString();
            string path = @$"{Desktop}\SFCFixScript.txt";
            string answer = "n";

            if (File.Exists(path)) {

                int file_count = Directory.EnumerateFiles(Desktop).Where(f => f.Contains("SFCFixScript")).Count();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: An existing SFCFixScript.txt file was found, do you wish to overwrite it [y/n]: ");
                answer = Console.ReadLine();
                Console.ResetColor();

                if (answer.ToLower() == "n")
                {
                    await File.WriteAllTextAsync(@$"{Desktop}\SFCFixScript({file_count + 1}).txt", lines);
                    return;
                }
            }

            await File.WriteAllTextAsync(path, lines);
        }

        private async Task BuildRegistryKeyScriptAsync(RegistryKey keys, string prefix, bool buildkey = false)
        {
            StringBuilder builder = new StringBuilder("::\n");
            string value_name = string.Empty;
            string key_name = string.Empty;

            string[] segments = KeyName.Split('\\');
            int length = segments.Length;

            if (buildkey)
            {
                key_name = segments[length - 1];
            }
            else
            {
                key_name = segments[length - 2];
                value_name = segments[length - 1];
            }

            RegistryKey key = keys.OpenSubKey(key_name);

            if (key is null)
            {
                Console.WriteLine($"Unable to find key: {key_name}");
                key.Close();
                return;
            }

            builder.AppendLine($"{prefix}{key_name}]");

            if (buildkey)
            {
                foreach (string value in key.GetValueNames())
                {
                    string line = BuildRegistryValue(key, value);
                    builder.AppendLine(line);
                }
            }
            else
            {
                string line = BuildRegistryValue(key, value_name);
                builder.AppendLine(line);
            }

            key.Close();

            await WriteSFCFixScriptAsync(builder);

        }

        private async Task BuildRegistryKeysScriptAsync(RegistryKey keys, string prefix, bool buildkey = false)
        {
            StringBuilder builder = new StringBuilder("::");

            string key_name = string.Empty;
            string value_name = string.Empty;
            string current_key = string.Empty;

            foreach (string source_line in await File.ReadAllLinesAsync(SourcePath))
            {
                string[] segments = source_line.Split('\\');
                int length = segments.Length;
                
                if (buildkey)
                {
                    key_name = segments[length - 1];
                }
                else
                {
                    key_name = segments[length - 2];
                    value_name = segments[length - 1];
                }

                RegistryKey key = keys.OpenSubKey(key_name);

                if (key is null)
                {
                    Console.WriteLine($"Unable to find key: {key_name}");
                    continue;
                }

                if (key_name != current_key)
                {
                    builder.AppendLine("\n");
                    builder.AppendLine($"{prefix}{key_name}]");
                    current_key = key_name;
                }

                if (buildkey)
                {
                    foreach (string value in key.GetValueNames())
                    {
                        string line = BuildRegistryValue(key, value);
                        builder.AppendLine(line);
                    }
                }
                else
                {
                    string line = BuildRegistryValue(key, value_name);
                    builder.AppendLine(line);
                }

                key.Close();
            }

            await WriteSFCFixScriptAsync(builder);
        }

        private string BuildRegistryValue(RegistryKey key, string value_name)
        {
            RegistryValueKind type = key.GetValueKind(value_name);
            object data = key.GetValue(value_name);
            string formatted_value = string.Empty;
            string value_data = string.Empty;

            switch (type)
            {
                case RegistryValueKind.DWord:
                    formatted_value = Convert.ToString(Convert.ToInt64(data), 16);
                    int padding_length = 8 - formatted_value.Length;

                    for (int i = 0; i < padding_length; i++)
                    {
                        formatted_value = string.Concat(formatted_value.Prepend('0'));
                    }

                    value_data = $"\"{value_name}\"=dword:{formatted_value}";
                    break;
                case RegistryValueKind.Binary:
                    byte[] hex_data = data as byte[];
                    formatted_value = BitConverter.ToString(hex_data)?.Replace("-", ",").ToLower();

                    value_data = $"\"{value_name}\"=hex:{formatted_value}";
                    break;
                case RegistryValueKind.String:
                    value_data = $"\"{value_name}\"={data}";
                    break;
                default:
                    value_data = $"\"{value_name}\"={Convert.ToString(Convert.ToInt64(data), 16)}";
                    break;
            }

            return value_data;
        }

        #endregion
    }

}


