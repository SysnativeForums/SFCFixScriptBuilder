# SFCFixScriptBuilder

SFCFixScriptBuilder is intended mostly for building corrupted registry keys from a Process Monitor log, although, it can used to build any set of keys, as long as, you provide the key names and their values (if applicable) within a .txt file. The tool will run through each line of .txt file, look up the registry key and its value within the specified source COMPONENTS hive and then write those values to the SFCFixScript.txt file which can then be used by the SFCFix tool.

Please use --help for usage instructions. These will be added to the README in due course.
