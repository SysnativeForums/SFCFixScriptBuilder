using Registry;
using SFCFixScriptBuilder.Constants;
using SFCFixScriptBuilder.Helpers;
using System.Text;
using System.Text.RegularExpressions;
using Registry.Abstractions;
using Registry.Cells;
using SFCFixScriptBuilder.Extensions;

namespace SFCFixScriptBuilder.RegistryScriptBuilder
{
    public class RegistryScriptBuilder
    {
        private RegistryHiveOnDemand _components;
        private RegistryHiveOnDemand _cbs;

        public RegistryScriptBuilder(ref readonly RegistryHiveOnDemand components, ref readonly RegistryHiveOnDemand cbs) 
        {
            _components = components;
            _cbs = cbs;
        }

        public RegistryScriptBuilder(ref readonly RegistryHiveOnDemand components)
        {
            _components = components;
            _cbs = default!;
        }

        private string BuildVMarks(RegistryKey componentFamily)
        {
            var builder = new StringBuilder('\n');
            var prefix = Prefixes.ComponentFamiliesPrefix;

            foreach (var vMark in componentFamily.SubKeys)
            {
                builder.AppendLine($@"{prefix}{componentFamily.KeyName}\{vMark.KeyName}]");

                foreach (var value in vMark.Values)
                {
                    var vMarkValue = BuildRegistryValue(value);
                    builder.AppendLine(vMarkValue);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private string BuildAssociatedDeployments(RegistryKey siblingKey)
        {
            var builder = new StringBuilder('\n');

            var deployments = siblingKey.Values
                .Where(kv => kv.ValueName.StartsWith("c!"))
                .Select(kv => kv.ValueName);

            foreach (var valueName in deployments)
            {
                // Trim the c! mark from the value name
                ReadOnlySpan<char> span = valueName;
                var deploymentName = span.Slice(2).ToString();

                var deployment = _components.GetKey($@"\CanonicalData\Deployments\{deploymentName}");

                if (deployment is null)
                {
                    ConsoleWriter.WriteMessage($"The deployment key: {deploymentName} does not exist", ConsoleColor.Yellow);
                    continue;
                }

                var prefix = Prefixes.DeploymentsPrefix;
                builder.AppendLine($"{prefix}{deploymentName}]");

                foreach (var value in deployment.Values)
                {
                    var deploymentValue = BuildRegistryValue(value);
                    builder.AppendLine(deploymentValue);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private string BuildAssociatedPackages(RegistryKey siblingKey)
        {
            var builder = new StringBuilder('\n');

            var packages = siblingKey.Values
                .Where(kv => kv.ValueName.StartsWith("p!"))
                .Select(kv => kv.ValueDataRaw);

            foreach (var rawPackageName in packages)
            {
                // Build the actual package name from the value data
                var characters = Encoding.UTF8.GetChars(rawPackageName!);
                var packageName = PrettifyPackageName(characters);

                var package = _cbs.GetKey($@"\Packages\{packageName}");

                if (package is null)
                {
                    ConsoleWriter.WriteMessage($"The package key: {packageName} does not exist", ConsoleColor.Yellow);
                    continue;
                }

                var prefix = Prefixes.PackagesPrefix;
                builder.AppendLine($"{prefix}{packageName}]");

                foreach (var value in package.Values)
                {
                    var packageValue = BuildRegistryValue(value);
                    builder.AppendLine(packageValue);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        public string BuildRegistryKeyScript(string keyPath, bool buildSiblings, bool isMulti)
        {
            var builder = new StringBuilder();

            if (!isMulti) builder.AppendLine("::");

            var key = _components.GetKey(keyPath) ?? _cbs?.GetKey(keyPath);

            if (key is null)
            {
                ConsoleWriter.WriteMessage($"The subkey: {keyPath} does not exist", ConsoleColor.Yellow);
                return string.Empty;
            }

            // Gather key information such as the prefix and sibling key type
            var keyInformation = GetKeyInformation(key);

            builder.AppendLine($"{keyInformation.Prefix}{key.KeyName}]");

            foreach (var value in key.Values)
            {
                var keyValue = BuildRegistryValue(value);
                builder.AppendLine(keyValue);
            }

            builder.AppendLine();

            // Component Families have subkeys which also need to be built
            if (keyInformation.Prefix.EndsWith(@"\ComponentFamilies\"))
            {
                var vMarks = BuildVMarks(key);
                builder.Append(vMarks);
            }

            if (buildSiblings)
            {
                var siblingKeys = BuildAssociatedKeys(keyInformation.SiblingKeyType, key);
                builder.Append(siblingKeys);
            }

            return builder.ToString();
        }

        private string BuildRegistryValue(KeyValue value)
        {
            var valueName = value.ValueName.StartsWith("(default)") ? "@" : value.ValueName;
            var type = value?.VkRecord.DataType;
            var keyData = value?.ValueData;
            
            var formattedValue = string.Empty;
            var valueData = string.Empty;

            switch (type)
            {
                case VkCellRecord.DataTypeEnum.RegDword:
                    formattedValue = Convert.ToString(Convert.ToInt64(keyData), 16);
                    var paddingLength = 8 - formattedValue.Length;

                    for (var i = 0; i < paddingLength; i++)
                    {
                        formattedValue = string.Concat(formattedValue.Prepend('0'));
                    }

                    valueData = $"\"{valueName}\"=dword:{formattedValue}";
                    break;
                case VkCellRecord.DataTypeEnum.RegBinary:
                    formattedValue = BitConverter.ToString(value?.ValueDataRaw!)?.Replace("-", ",").ToLower();
                    
                    if (formattedValue?.Length > 63)
                    {
                        formattedValue = formattedValue.FormatRegBinary(formattedValue.Length - 1);
                    }

                    valueData = $"\"{valueName}\"=hex:{formattedValue}";
                    break;
                case VkCellRecord.DataTypeEnum.RegSz:
                    valueData = $"\"{valueName}\"={keyData}";
                    break;
                default:
                    valueData = $"\"{valueName}\"={Convert.ToString(Convert.ToInt64(keyData), 16)}";
                    break;
            }

            return valueData?.Trim()!;
        }

        private string BuildAssociatedKeys(SiblingKeyType type, RegistryKey siblingKey)
        {
            return type switch
            {
                SiblingKeyType.Deployment => BuildAssociatedDeployments(siblingKey),
                SiblingKeyType.Package => BuildAssociatedPackages(siblingKey),
                _ => string.Empty
            };
        }

        private KeyInformation GetKeyInformation(RegistryKey key)
        {
            return new KeyInformation
            {
                SiblingKeyType = GetSiblingKeyType(key),
                Prefix = GetRegistryKeyPrefix(key)
            };
        }

        private string GetRegistryKeyPrefix(RegistryKey key)
        {
            var comparsionType = StringComparison.InvariantCultureIgnoreCase;

            return key.KeyPath switch
            {
                string k when k.Contains("Packages", comparsionType) => Prefixes.PackagesPrefix,
                string k when k.Contains("PackageIndex", comparsionType) => Prefixes.PackageIndexPrefix,
                string k when k.Contains("PackageDetect", comparsionType) => Prefixes.PackageDetectPrefix,
                string k when k.Contains("ComponentDetect", comparsionType) => Prefixes.ComponentDetectPrefix,
                string k when k.Contains("Catalogs", comparsionType) => Prefixes.CatalogsPrefix,
                string k when k.Contains("Deployments", comparsionType) => Prefixes.DeploymentsPrefix,
                string k when k.Contains("Components", comparsionType) => Prefixes.ComponentsPrefix,
                string k when k.Contains("ComponentFamilies", comparsionType) => Prefixes.ComponentFamiliesPrefix,
                _ => string.Empty
            };
        }

        private SiblingKeyType GetSiblingKeyType(RegistryKey key)
        {
            var comparsionType = StringComparison.InvariantCultureIgnoreCase;

            return key.KeyPath switch
            {
                string k when k.Contains("Packages", comparsionType) => SiblingKeyType.PackageIndex,
                string k when k.Contains("Deployments", comparsionType) => SiblingKeyType.Package,
                string k when k.Contains("Components", comparsionType) => SiblingKeyType.Deployment,
                _ => SiblingKeyType.None
            };
        }

        private string PrettifyPackageName(char[] package)
        {
            var span = new ReadOnlySpan<char>(package);
            var controlCharIndex = span.LastIndexOf('\0');

            var firstSlice = span.Slice(controlCharIndex + 1);

            var trimIndex = firstSlice.LastIndexOf('.');
            var secondSlice = firstSlice.Slice(0, trimIndex);

            return secondSlice.ToString();
        }
    }

}