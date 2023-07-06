using Microsoft.Win32;
using SFCFixScriptBuilder.Constants;
using SFCFixScriptBuilder.RegistryHiveLoader;
using System.Text;
using System.Text.RegularExpressions;
using static System.Environment;

namespace SFCFixScriptBuilder.RegistryScriptBuilder
{
    public class RegistryScriptBuilder
    {
        private readonly RegistryKey HKLM = HiveLoader.HKLM;
        private readonly string Desktop = $@"{GetEnvironmentVariable("userprofile")}\Desktop";
        
        private string LogPath = string.Empty;
        private string KeyName = string.Empty;
        private string Version = string.Empty;

        public RegistryScriptBuilder(string logPath, string key, string version) 
        {
            LogPath = logPath;
            KeyName = key;
            Version = version;
        }

        private string BuildAssociatedDeployment(string deployment_name)
        {
            RegistryKey deployments = HKLM.OpenSubKey($@"{RegistryConfig.COMPONENTS}\CanonicalData\Deployments");
            StringBuilder builder = new StringBuilder();

            string prefix = Prefixes.DeploymentsPrefix;

            RegistryKey deployment = deployments.OpenSubKey(deployment_name.Replace("c!", string.Empty));

            if (deployment is null)
            {
                Console.WriteLine($"The deployment key: {deployment_name} does not exist");
                return string.Empty;
            }

            builder.AppendLine(string.Empty);
            builder.AppendLine($"{prefix}{deployment_name}]");

            foreach (string value in deployment.GetValueNames())
            {
                string line = BuildRegistryValue(deployment, value);
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        private string BuildAssociatedPackage(string package_name, RegistryKey parent_key)
        {            
            RegistryKey packages = HKLM.OpenSubKey($@"{RegistryConfig.CBS}\Packages");
            StringBuilder builder = new StringBuilder();

            //Need to loop over the values, get the actual package name and add those to package_names
            string prefix = Prefixes.PackagesPrefix;

            byte[] value_data = parent_key.GetValue(package_name) as byte[];
            string decoded_name = Encoding.ASCII.GetString(value_data);

            decoded_name = Regex.Replace(decoded_name, ".{1}.+\\0{1,7}", string.Empty);
            string version = Regex.Match(decoded_name, "\\d{1,4}\\.\\d{1,4}\\.\\d{1,5}\\.\\d{1,4}").Value;
            decoded_name = Regex.Replace(decoded_name, "(?>~~).*", string.Empty);
            package_name = $"{decoded_name}~~{version}";

            //Need to decode the actual package name from the value data itself
            RegistryKey package = packages.OpenSubKey(package_name);

            builder.AppendLine(string.Empty);
            builder.AppendLine($"{prefix}{package_name}]");

            foreach (string value in package.GetValueNames())
            {
                string line = BuildRegistryValue(package, value);
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        private SiblingKeyType GetSiblingKeyType(string prefix) => prefix switch
        {
            string p when p == Prefixes.ComponentsPrefix => SiblingKeyType.Deployment,
            string p when p == Prefixes.DeploymentsPrefix => SiblingKeyType.Package,
            _ => SiblingKeyType.None
        };

        public async Task BuildMissingKeysAsync(string path, string prefix, bool buildkey = false, bool sibling_mode = false)
        {
            RegistryKey keys = HKLM.OpenSubKey(path);
            SiblingKeyType type = sibling_mode ? GetSiblingKeyType(prefix) : SiblingKeyType.None;

            if (string.IsNullOrWhiteSpace(KeyName))
            {
                await BuildRegistryKeysScriptAsync(keys, prefix, buildkey, type);
            }
            else
            {
                await BuildRegistryKeyScriptAsync(keys, prefix, type);
            }

            CloseKeys(keys);
        }

        public async Task BuildMissingComponentFamiliesAsync(bool buildKey)
        {
            string prefix = Prefixes.ComponentFamiliesPrefix.Replace("{Version}", Version);
            RegistryKey versioned_index = HKLM.OpenSubKey(@$"{RegistryConfig.COMPONENTS}\DerivedData\VersionedIndex");

            foreach (string version in versioned_index.GetSubKeyNames())
            {
                RegistryKey component_families = HKLM.OpenSubKey(@$"{RegistryConfig.COMPONENTS}\DerivedData\VersionedIndex\{version}\ComponentFamilies");

                if (string.IsNullOrWhiteSpace(KeyName))
                {
                    await BuildRegistryKeysScriptAsync(component_families, prefix, buildKey);
                }
                else
                {
                    await BuildRegistryKeyScriptAsync(component_families, prefix);
                }

                CloseKeys(component_families);
            }

            CloseKeys(versioned_index);
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
                Console.Write("Warning: An existing SFCFixScript.txt file was found, do you wish to overwrite it [y/n]: ");
                answer = Console.ReadLine();
                Console.ResetColor();

                if (answer.ToLower() == "n")
                {
                    await File.WriteAllTextAsync(@$"{Desktop}\SFCFixScript ({file_count + 1}).txt", lines);
                    return;
                }
            }

            await File.WriteAllTextAsync(path, lines);
        }

        private async Task BuildRegistryKeyScriptAsync(RegistryKey keys, string prefix, SiblingKeyType type = SiblingKeyType.None)
        {
            StringBuilder builder = new StringBuilder("::\n");
            StringBuilder associated_keys = new StringBuilder();
            Regex pattern = new Regex("[cisp]\\!");

            HashSet<string> checked_keys = new HashSet<string>();

            string[] segments = KeyName.Split('\\');
            int length = segments.Length;
            string key_name = segments[length - 1];

            RegistryKey key = keys.OpenSubKey(key_name);

            if (key is null)
            {
                Console.WriteLine($"Unable to find key: {key_name}");
                key.Close();
                return;
            }

            builder.AppendLine($"{prefix}{key_name}]");

            foreach (string value in key.GetValueNames())
            {
                string line = BuildRegistryValue(key, value);
                builder.AppendLine(line);

                if (type is not SiblingKeyType.None && pattern.IsMatch(value))
                {
                    if (checked_keys.Contains(pattern.Replace(value, string.Empty))) continue;

                    string checked_key = pattern.Replace(value, string.Empty);
                    checked_keys.Add(checked_key);

                    string associated_key = BuildAssociatedKey(type, value, key);

                    if (!string.IsNullOrWhiteSpace(associated_key)) associated_keys.Append(associated_key);
                }
            }

            key.Close();

            if (!string.IsNullOrWhiteSpace(associated_keys.ToString()))
            {
                builder.Append(associated_keys.ToString());
            }

            await WriteSFCFixScriptAsync(builder);

        }

        private async Task BuildRegistryKeysScriptAsync(RegistryKey keys, string prefix, bool buildkey, SiblingKeyType type = SiblingKeyType.None)
        {
            StringBuilder builder = new StringBuilder("::");
            StringBuilder associated_keys = new StringBuilder();

            Regex pattern = new Regex("[cisp]\\!");

            //Maintain a list of values which have already been checked to avoid duplication in fix script
            HashSet<string> checked_keys = new HashSet<string>();

            string key_name;
            string value_name = string.Empty;
            string current_key = string.Empty;

            foreach (string source_line in await File.ReadAllLinesAsync(LogPath))
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
                    builder.AppendLine(string.Empty);
                    builder.AppendLine($"{prefix}{key_name}]");
                    current_key = key_name;
                }

                if (buildkey)
                {
                    foreach (string value in key.GetValueNames())
                    {
                        string line = BuildRegistryValue(key, value);
                        builder.AppendLine(line);

                        if (type is not SiblingKeyType.None && pattern.IsMatch(value))
                        {
                            if (checked_keys.Contains(pattern.Replace(value, string.Empty))) continue;

                            string checked_key = pattern.Replace(value, string.Empty);
                            checked_keys.Add(checked_key);

                            string associated_key = BuildAssociatedKey(type, value, key);

                            if (!string.IsNullOrWhiteSpace(associated_key)) associated_keys.Append(associated_key);
                        }
                    }
                }
                else
                {
                    string line = BuildRegistryValue(key, value_name);
                    builder.AppendLine(line);
                }

                key.Close();
            }

            if (!string.IsNullOrWhiteSpace(associated_keys.ToString()))
            {
                builder.Append(associated_keys.ToString());
            }

            await WriteSFCFixScriptAsync(builder);
        }

        private string BuildRegistryValue(RegistryKey key, string value_name)
        {
            RegistryValueKind type = key.GetValueKind(value_name);
            object data = key.GetValue(value_name);
            string formatted_value;
            string value_data;

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

            return value_data?.Trim();
        }

        private string BuildAssociatedKey(SiblingKeyType type, string key_name, RegistryKey parent_key = null)
        {
            switch (type)
            {
                case SiblingKeyType.Deployment:
                    return BuildAssociatedDeployment(key_name);
                case SiblingKeyType.Package:
                    return BuildAssociatedPackage(key_name, parent_key);
                default:
                    return string.Empty;
            }
        }

        #endregion
    }

}


