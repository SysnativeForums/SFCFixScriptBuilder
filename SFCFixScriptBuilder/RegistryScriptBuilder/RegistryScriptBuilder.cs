using Microsoft.Win32;
using SFCFixScriptBuilder.Constants;
using SFCFixScriptBuilder.RegistryHiveLoader;
using System.Text;
using System.Text.RegularExpressions;

namespace SFCFixScriptBuilder.RegistryScriptBuilder
{
    public class RegistryScriptBuilder
    {
        private RegistryKey HKLM = HiveLoader.HKLM;
        private const string COMPONENTS = "SOURCE";
        string SourcePath { get; set; }

        public RegistryScriptBuilder(string sourcePath) 
        {
            SourcePath = sourcePath;
        }

        public async Task BuildAsync()
        {
            string prefix = Prefixes.ComponentsPrefix;

            RegistryKey component_families = HKLM.OpenSubKey(@$"SOURCE\DerivedData\Components");
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

                byte[] value = component.GetValue(s256h) as byte[];

                builder.AppendLine($"\"{s256h}\"=hex:{BitConverter.ToString(value)?.Replace("-", ",").ToLower()}");
            }

            string lines = builder.ToString();
            await File.WriteAllTextAsync(@"C:\Users\bsodt\Desktop\SFCFixScript2.txt", lines);
        }

        public async Task BuildMissingFMarksScript()
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

                object value = component.GetValue(f_mark);
                string dword_data = Convert.ToString(Convert.ToInt64(value), 16);
                int padding_length = 8 - dword_data.Length;

                for (int i = 0; i < padding_length; i++)
                {
                    dword_data = string.Concat(dword_data.Prepend('0'));
                }

                builder.AppendLine($"\"{f_mark}\"=dword:{dword_data}");
            }

            string lines = builder.ToString();
            await File.WriteAllTextAsync(@"C:\Users\bsodt\Desktop\SFCFixScript.txt", lines);
        }
    }

}


