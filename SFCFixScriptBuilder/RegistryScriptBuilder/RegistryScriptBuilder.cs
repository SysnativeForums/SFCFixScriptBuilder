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
        private RegistryKey HKLM = HiveLoader.HKLM;
        private const string COMPONENTS = "SOURCE";
        private string Desktop = $@"{GetEnvironmentVariable("userprofile")}\Desktop";
        string SourcePath { get; set; }

        public RegistryScriptBuilder(string sourcePath) 
        {
            SourcePath = sourcePath;
        }

        public async Task BuildMissingS256HMarksScriptAsync()
        {
            string prefix = Prefixes.ComponentsPrefix;

            RegistryKey component_families = HKLM.OpenSubKey(@$"{COMPONENTS}\DerivedData\Components");
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("::\n");

            string component_name = string.Empty;
            string current_component = string.Empty;

            foreach (string source_line in await File.ReadAllLinesAsync(SourcePath))
            {
                //Extract S256H mark and then remove it
                string s256h = Regex.Match(source_line, "(?=S256H).*").Value;
                component_name = Regex.Replace(source_line, "(?=S256H).*", string.Empty).Replace("\\", string.Empty);

                RegistryKey component = component_families.OpenSubKey(component_name);

                if (component is null)
                {
                    Console.WriteLine($"Unable to find component family: {component_name}");
                    continue;
                }

                if (component_name != current_component)
                {
                    builder.AppendLine($"{prefix}{component_name}]");
                    current_component = component_name;
                }

                string line = BuildRegistryValue(component, s256h);
                builder.AppendLine(line);

                component.Close();
            }

            string lines = builder.ToString();
            await File.WriteAllTextAsync(@$"{Desktop}\SFCFixScript.txt", lines);

            component_families.Close();
            HKLM.Close();
        }

        public async Task BuildMissingFMarksScriptAsync()
        {
            RegistryKey components = HKLM.OpenSubKey(@$"{COMPONENTS}\DerivedData\Components");
            StringBuilder builder = new StringBuilder("::\n");

            string prefix = Prefixes.ComponentsPrefix;

            string component_name = "";
            string current_component = "";

            foreach (string source_line in await File.ReadAllLinesAsync(SourcePath))
            {
                //Extract f! mark and then remove it
                string f_mark = Regex.Match(source_line, Patterns.F_Mark).Value;
                component_name = Regex.Replace(source_line, Patterns.F_Mark, string.Empty).Replace("\\", string.Empty);

                RegistryKey component = components.OpenSubKey(component_name);

                if (component is null)
                {
                    Console.WriteLine($"Unable to find component: {component_name}");
                    continue;
                }

                if (component_name != current_component)
                {
                    builder.AppendLine($"{prefix}{component_name}]");
                    current_component = component_name;
                }

                string line = BuildRegistryValue(component, f_mark);
                builder.AppendLine(line);
                
                component.Close();
            }

            string lines = builder.ToString();
            await File.WriteAllTextAsync(@$"{Desktop}\SFCFixScript.txt", lines);
            
            //Close any handles to keys otherwise the hive will be unable to be unloaded
            components.Close();
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
                default:
                    value_data = $"\"{value_name}\"={Convert.ToString(Convert.ToInt64(data), 16)}";
                    break;
            }

            return value_data;
        }
    }

}


