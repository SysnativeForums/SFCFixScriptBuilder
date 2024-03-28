# SFCFixScriptBuilder

SFCFixScriptBuilder is intended mostly for building corrupted registry keys from a Process Monitor log, although, it can used to build any set of keys, as long as, you provide the relative key names within a .txt file. The tool will run through each line of .txt file, look up the registry key within the specified source COMPONENTS hive and then write those values to the SFCFixScript.txt file which can then be used by the SFCFix tool.

There is two primary methods to running SFCFixScriptBuilder, the key method and the log file method. If you wish to repair a particular key, then it is recommended that you use the following:

SFCFixScriptBuilder -key <relative_key_path> --siblings

The --siblings option is entirely optional and does not apply to every key. This tends to be a very handy option when you wish to build a component and associated deployment key at the same time.

For example, if I wanted to build the component key in full along with it's deployment key, then I could use the following: 

SFCFixScriptBuilder -key \DerivedData\Components\amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.22621.2506_none_270c5ae97388e100 --siblings

On the other hand, if I wanted to build a set of component keys, then I could provide a log file which has the key names and use the following syntax:

SFCFixScriptBuilder -log <log_file_path> --siblings

The log file will need to contain the relative key paths as before. If you provide both the -key and -log options then -key will take precedence and only that key will be build. If you do not provide a key path to -key then it will build the values underneath the root of the hive file.

If you wish to build packages or any other keys which are part of the Component Based Servicing subkey, then you will need to provide the path to a .hiv file of the Component Based Servicing subkey to the -cbs option.

You can export a subkey as a .hiv file using: reg export "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing" %userprofile%\Desktop\CBS.hiv /y

Options:

-components: The path to the COMPONENTS hive file you wish to source the keys from. This is an optional parameter, but no path has been provided, then the COMPONENTS hive of the current system will be used instead.

-cbs: The path to the CBS hive file you wish to source the keys from. This is an optional parameter; it will not fallback to the CBS subkey of the running machine.

-log: The path to the .txt file which contains the relative registry key paths of all the keys you wish to build.

-key: The relative path of the registry key you wish to build.

--siblings: Determines if you wish to build the sibling keys associated to either the key provided to -key or the list of keys provided in the .txt file given in -log.

You must select either -key or -log!
