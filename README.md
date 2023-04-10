# SFCFixScriptBuilder

SFCFixScriptBuilder is intended mostly for building corrupted registry keys from a Process Monitor log, although, it can used to build any set of keys, as long as, you provide the key names and their values (if applicable) within a .txt file. The tool will run through each line of .txt file, look up the registry key and its value within the specified source COMPONENTS hive and then write those values to the SFCFixScript.txt file which can then be used by the SFCFix tool.

Instructions:

1. Prepare the .txt file - referred to as the log file in the tool - as a list of registry key paths. If you just intend to rebuild particular values then you must provide the value names.

Examples:

--> HKLM\COMPONENTS\CanonicalData\Deployments\microsoft-w..-deployment_31bf3856ad364e35_10.0.17763.1_93fba37bec31cb9b (Key Only)
--> HKLM\COMPONENTS\CanonicalData\Deployments\microsoft-w..-deployment_31bf3856ad364e35_10.0.17763.1_93fba37bec31cb9b\p!CBS_microsoft-windows-netfx4-oc-package~31bf3856ad364e35~amd64~~_13acec2cc6eef42a (Value Only)

2. Run the tool from an elevated command prompt using: sfcfixscriptbuilder -c <Path to COMPONENTS> -l <Path to .txt file>

Remember, if the .exe has been added to your PATH environment variable then you will need to open Command Prompt from the same directory as the tool.

3. Select the appropriate option from the tool's menu when prompted.
