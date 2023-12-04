using Microsoft.Win32;
using SFCFixScriptBuilder.Constants;
using SFCFixScriptBuilder.Helpers;
using SFCFixScriptBuilder.RegistryHiveLoader;
using System.Text;
using System.Text.RegularExpressions;
using static System.Environment;

namespace SFCFixScriptBuilder.RegistryScriptBuilder
{
    public class RegistryScriptBuilder
    {
        private readonly RegistryKey HKLM = HiveLoader.HKLM;
        private readonly string _desktop = $@"{GetEnvironmentVariable("userprofile")}\Desktop";
        
        private string _logPath = string.Empty;
        private string _keyName = string.Empty;
        private string _version = string.Empty;

        public RegistryScriptBuilder(string logPath, string key, string version) 
        {
            _logPath = logPath;
            _keyName = key;
            _version = version;
        }

        private string BuildAssociatedDeployment(string deployment_name)
        {
            var deployments = HKLM.OpenSubKey($@"{RegistryConfig.COMPONENTS}\CanonicalData\Deployments");
            var builder = new StringBuilder();

            var prefix = Prefixes.DeploymentsPrefix;

            var deployment = deployments.OpenSubKey(deployment_name.Replace("c!", string.Empty));

            if (deployment is null)
            {
                Console.WriteLine($"The deployment key: {deployment_name} does not exist");
                return string.Empty;
            }

            builder.AppendLine(string.Empty);
            builder.AppendLine($"{prefix}{deployment_name}]");

            foreach (var value in deployment.GetValueNames())
            {
                var line = BuildRegistryValue(deployment, value);
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        private string BuildAssociatedPackage(string packageName, RegistryKey parent_key)
        {            
            var packages = HKLM.OpenSubKey($@"{RegistryConfig.CBS}\Packages");
            var builder = new StringBuilder();

            //Need to loop over the values, get the actual package name and add those to packageNames
            var prefix = Prefixes.PackagesPrefix;

            byte[] valueData = parent_key.GetValue(packageName) as byte[];
            var decodedName = Encoding.ASCII.GetString(valueData);

            decodedName = Regex.Replace(decodedName, ".{1}.+\\0{1,7}", string.Empty);
            var version = Regex.Match(decodedName, "\\d{1,4}\\.\\d{1,4}\\.\\d{1,5}\\.\\d{1,4}").Value;
            decodedName = Regex.Replace(decodedName, "(?>~~).*", string.Empty);
            packageName = $"{decodedName}~~{version}";

            //Need to decode the actual package name from the value data itself
            var package = packages.OpenSubKey(packageName);

            builder.AppendLine(string.Empty);
            builder.AppendLine($"{prefix}{packageName}]");

            foreach (var value in package.GetValueNames())
            {
                var line = BuildRegistryValue(package, value);
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        private SiblingKeyType GetSiblingKeyType(string prefix) => prefix switch
        {
            var p when p == Prefixes.ComponentsPrefix => SiblingKeyType.Deployment,
            var p when p == Prefixes.DeploymentsPrefix => SiblingKeyType.Package,
            _ => SiblingKeyType.None
        };

        public async Task BuildMissingKeysAsync(string path, string prefix, bool buildkey = false, bool sibling_mode = false)
        {
            var keys = HKLM.OpenSubKey(path);
            var type = sibling_mode ? GetSiblingKeyType(prefix) : SiblingKeyType.None;

            if (string.IsNullOrWhiteSpace(_keyName))
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
            var prefix = Prefixes.ComponentFamiliesPrefix.Replace("{Version}", _version);
            var versioned_index = HKLM.OpenSubKey(@$"{RegistryConfig.COMPONENTS}\DerivedData\VersionedIndex");

            foreach (string version in versioned_index.GetSubKeyNames())
            {
                var componentFamilies = HKLM.OpenSubKey(@$"{RegistryConfig.COMPONENTS}\DerivedData\VersionedIndex\{version}\ComponentFamilies");

                if (string.IsNullOrWhiteSpace(_keyName))
                {
                    await BuildRegistryKeysScriptAsync(componentFamilies, prefix, buildKey);
                }
                else
                {
                    await BuildRegistryKeyScriptAsync(componentFamilies, prefix);
                }

                CloseKeys(componentFamilies);
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
            var lines = builder.ToString();
            var path = @$"{_desktop}\SFCFixScript.txt";
            var answer = "n";

            if (File.Exists(path)) {

                var file_count = Directory.EnumerateFiles(_desktop).Where(f => f.Contains("SFCFixScript")).Count();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Warning: An existing SFCFixScript.txt file was found, do you wish to overwrite it [y/n]: ");
                answer = Console.ReadLine();
                Console.ResetColor();

                if (answer?.ToLower() == "n")
                {
                    await File.WriteAllTextAsync(@$"{_desktop}\SFCFixScript ({file_count + 1}).txt", lines);
                    return;
                }
            }

            await File.WriteAllTextAsync(path, lines);
        }

        private async Task BuildRegistryKeyScriptAsync(RegistryKey keys, string prefix, SiblingKeyType type = SiblingKeyType.None)
        {
            var builder = new StringBuilder("::\n");
            var associatedKeys = new StringBuilder();
            var pattern = new Regex("[cisp]\\!");

            var checkedKeys = new HashSet<string>();

            string[] segments = _keyName.Split('\\');
            var length = segments.Length;
            var keyName = segments[length - 1];

            var key = keys.OpenSubKey(keyName);

            if (key is null)
            {
                Console.WriteLine($"Unable to find key: {keyName}");
                key.Close();
                return;
            }

            builder.AppendLine($"{prefix}{keyName}]");

            foreach (var value in key.GetValueNames())
            {
                var line = BuildRegistryValue(key, value);
                builder.AppendLine(line);

                if (type is not SiblingKeyType.None && pattern.IsMatch(value))
                {
                    if (checkedKeys.Contains(pattern.Replace(value, string.Empty))) continue;

                    var checkedKey = pattern.Replace(value, string.Empty);
                    checkedKeys.Add(checkedKey);

                    var associated_key = BuildAssociatedKey(type, value, key);

                    if (!string.IsNullOrWhiteSpace(associated_key)) associatedKeys.Append(associated_key);
                }
            }

            key.Close();

            if (!string.IsNullOrWhiteSpace(associatedKeys.ToString()))
            {
                builder.Append(associatedKeys.ToString());
            }

            await WriteSFCFixScriptAsync(builder);

        }

        private async Task BuildRegistryKeysScriptAsync(RegistryKey keys, string prefix, bool buildkey, SiblingKeyType type = SiblingKeyType.None)
        {
            var builder = new StringBuilder("::");
            var associatedKeys = new StringBuilder();

            var pattern = new Regex("[cisp]\\!");

            //Maintain a list of values which have already been checked to avoid duplication in fix script
            var checkedKeys = new HashSet<string>();

            var keyName = string.Empty;
            var valueName = string.Empty;
            var currentKey = string.Empty;

            foreach (var sourceLine in await File.ReadAllLinesAsync(_logPath))
            {
                string[] segments = sourceLine.Split('\\');
                var length = segments.Length;
                
                if (buildkey)
                {
                    keyName = segments[length - 1];
                }
                else
                {
                    keyName = segments[length - 2];
                    valueName = segments[length - 1];
                }

                var key = keys.OpenSubKey(keyName);

                if (key is null)
                {
                    Console.WriteLine($"Unable to find key: {keyName}");
                    continue;
                }

                if (keyName != currentKey)
                {
                    builder.AppendLine(string.Empty);
                    builder.AppendLine($"{prefix}{keyName}]");
                    currentKey = keyName;
                }

                if (buildkey)
                {
                    foreach (var value in key.GetValueNames())
                    {
                        var line = BuildRegistryValue(key, value);
                        builder.AppendLine(line);

                        if (type is not SiblingKeyType.None && pattern.IsMatch(value))
                        {
                            if (checkedKeys.Contains(pattern.Replace(value, string.Empty))) continue;

                            var checkedKey = pattern.Replace(value, string.Empty);
                            checkedKeys.Add(checkedKey);

                            var associated_key = BuildAssociatedKey(type, value, key);

                            if (!string.IsNullOrWhiteSpace(associated_key)) associatedKeys.Append(associated_key);
                        }
                    }
                }
                else
                {
                    var line = BuildRegistryValue(key, valueName);
                    builder.AppendLine(line);
                }

                key.Close();
            }

            if (!string.IsNullOrWhiteSpace(associatedKeys.ToString()))
            {
                builder.Append(associatedKeys.ToString());
            }

            await WriteSFCFixScriptAsync(builder);
        }

        private string BuildRegistryValue(RegistryKey key, string valueName)
        {
            var type = key.GetValueKind(valueName);
            var data = key.GetValue(valueName);
            var formattedValue = string.Empty;
            var valueData = string.Empty;

            switch (type)
            {
                case RegistryValueKind.DWord:
                    formattedValue = Convert.ToString(Convert.ToInt64(data), 16);
                    var paddingLength = 8 - formattedValue.Length;

                    for (var i = 0; i < paddingLength; i++)
                    {
                        formattedValue = string.Concat(formattedValue.Prepend('0'));
                    }

                    valueData = $"\"{valueName}\"=dword:{formattedValue}";
                    break;
                case RegistryValueKind.Binary:
                    byte[] hex_data = data as byte[];
                    formattedValue = BitConverter.ToString(hex_data)?.Replace("-", ",").ToLower();
                    
                    if (formattedValue.Length > 63)
                    {
                        formattedValue = Formatter.FormatRegBinary(formattedValue, formattedValue.Length - 1);
                    }

                    valueData = $"\"{valueName}\"=hex:{formattedValue}";
                    break;
                case RegistryValueKind.String:
                    valueData = $"\"{valueName}\"={data}";
                    break;
                default:
                    valueData = $"\"{valueName}\"={Convert.ToString(Convert.ToInt64(data), 16)}";
                    break;
            }

            return valueData?.Trim();
        }

        private string BuildAssociatedKey(SiblingKeyType type, string keyName, RegistryKey parent_key = null)
        {
            switch (type)
            {
                case SiblingKeyType.Deployment:
                    return BuildAssociatedDeployment(keyName);
                case SiblingKeyType.Package:
                    return BuildAssociatedPackage(keyName, parent_key);
                default:
                    return string.Empty;
            }
        }

        #endregion
    }

}


