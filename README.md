# SFCFixScriptBuilder

SFCFixScriptBuilder is intended mostly for building corrupted registry keys from a Process Monitor log, although, it can used to build any set of keys, as long as, you provide the key names and their values (if applicable) within a .txt file. The tool will run through each line of .txt file, look up the registry key and its value within the specified source COMPONENTS hive and then write those values to the SFCFixScript.txt file which can then be used by the SFCFix tool.

There is two primary methods to running SFCFixScriptBuilder, the key method and the log file method. If you wish to repair a particular key, then it is recommended that you use the following:

SFCFixScriptBuilder -key <component key name> <key type> <option>

For example, if I wanted to build the associated key in full along with it's deployment key, then I could use the following: 

SFCFixScriptBuilder -key amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.22621.2506_none_270c5ae97388e100 --components --siblings

On the other hand, if I wanted to build a set of component keys, then I could provide a log file which has the key names and use the following syntax:

SFCFixScriptBuilder -log <log file path> --components --full

It is important to include the --full switch to indicate that you wish to build a key rather than a key value. By default, with the log file option, SFCFixScriptBuilder will assume that you're trying to build a set of key values. However, if that was the case, within the log file, please provide the value name in your key path and then omit --full.

Please use --help for full usage instructions.
