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
        private string Desktop = $@"{GetEnvironmentVariable("userprofile")}\Desktop";
        string SourcePath { get; set; }
        public string Version { get; set; }

        public RegistryScriptBuilder(string sourcePath) 
        {
            SourcePath = sourcePath;
        }

        public async Task BuildMissingComponentValuesAsync(bool buildkey = false)
        {
            RegistryKey components = HKLM.OpenSubKey(@$"{COMPONENTS}\DerivedData\Components");
            await BuildRegistryScriptAsync(components, Prefixes.ComponentsPrefix, buildkey);
        }

        public async Task BuildMissingDeploymentValuesAsync(bool buildkey = false)
        {
            RegistryKey deployments = HKLM.OpenSubKey($@"{COMPONENTS}\CanonicalData\Deployments");
            await BuildRegistryScriptAsync(deployments, Prefixes.DeploymentsPrefix, buildkey);
        }

        public async Task BuildMissingComponentFamilyValuesAsync(bool buildKey = false)
        {
            string prefix = Prefixes.ComponentFamiliesPrefix.Replace("{Version}", Version);

            RegistryKey component_families = HKLM.OpenSubKey(@$"{COMPONENTS}\DerivedData\VersionedIndex\{Version}\ComponentFamilies");
            await BuildRegistryScriptAsync(component_families, prefix, buildKey);
        }

        private async Task BuildRegistryScriptAsync(RegistryKey keys, string prefix, bool buildkey = false)
        {
            StringBuilder builder = new StringBuilder("::\n");

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

            string lines = builder.ToString();
            await File.WriteAllTextAsync(@$"{Desktop}\SFCFixScript.txt", lines);

            //Close any handles to keys otherwise the hive will be unable to be unloaded
            keys.Close();
            HKLM.Close();
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
    }

}


