## Logging to Visual Studio Activity Log

When starting visual from command line, one can pass `/log` parameter to enable writing to activity log of Visual Studio.

Activity logs are located in `%APPDATA%\Microsoft\VisualStudio`, in appropriate catalog corresponding to VS version, under the name of `ActivityLog.xml`

In case of any problems with package, it is useful for users/developers to try run VS with logging enabled.

### VSIX project and experimental instance

In order to enable this by default in VS, it was changed in VSIX project under project properties, debug tab in "Command line arguments" section (added `/log` parameter).