using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AddTranslationCore.ResourceClassGenerator
{
#error File is not compiled into output DLLs as another approach is chosen. But implementation is left, as it is partially tested and designed. Class itself contains quite knowledge about the ResGen tool.
    /// <summary>
    /// Visual Studio uses ResGen.exe utility to generate strongly typed class for resources.
    /// It usually is located in one of Windows SDK subdirectories and is shipped with Visual Studio.
    /// </summary>
    class ResGenWrapper
    {
        public delegate void ResGenFileNotFoundDelegate(string searchedDirectory);
        public event ResGenFileNotFoundDelegate ResGenFileNotFound;
        public delegate void FoundMultipleResGenFilesDelegate(string[] foundFiles);
        public event FoundMultipleResGenFilesDelegate FoundMultipleResGenFiles;
        /// <summary>
        /// Default ResGen file name
        /// </summary>
        private const string RESGEN_EXECUTABLE = "ResGen.exe";
        /// <summary>
        /// Directory, where ResGen should be located (or in one of its subdirectories).
        /// </summary>
        private const string WINDOWS_SDK_DIRECTORY = @"C:\Program Files (x86)\Microsoft SDKs\Windows";
        /// <summary>
        /// Arguments passed to ResGen process. Those are as follows:
        /// - {0} - full path to resource file,
        /// - {1} - namespace, that should be used,
        /// - {2} - class name, that should be used.
        /// It also can generate strongly typed class in other languages than C#. Here we specify
        /// C# with "cs" after /str: argument.
        /// For details see <see href="https://docs.microsoft.com/en-us/dotnet/framework/tools/resgen-exe-resource-file-generator">MS docs</see>
        /// </summary>
        private const string resgenArgsFormat = "{0} /str:cs,{1},{2}";
        private string _customResGenFilePath;
        private readonly ILog _logger;

        public ResGenWrapper()
        {
            _logger = LogManager.GetLogger(nameof(ResGenWrapper));
        }

        public bool AutoResolveMultipleFiles { get; set; }
        public bool UseCustomResGenFilePath { get; set; }
        public string CustomResGenFilePath
        {
            get => _customResGenFilePath;
            set
            {
                _logger.Debug($"Setting custom file path to {value}");
                _customResGenFilePath = value;
            }
        }

        public bool GenerateResourcesClass(string resourcesPath, string @namespace, string @class)
        {
            var resGenExe = string.Empty;
            if (!TryGetResGenExecutable(out resGenExe))
                return false;

            var args = string.Format(resgenArgsFormat, resourcesPath, @namespace, @class);
            try
            {
                _logger.Info($"Starting Resource Generator process from path {resGenExe} with args: {args}");
                var resgenProcess = Process.Start(resGenExe, args);
                resgenProcess.WaitForExit();
                if (resgenProcess.ExitCode != 0)
                {
                    _logger.Error($"ResGen process failed and returned with code {resgenProcess.ExitCode}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception during ResGen process.", ex);
                return false;
            }
        }

        private bool TryGetResGenExecutable(out string resgenExe)
        {
            _logger.Info($"Searching for {RESGEN_EXECUTABLE}");
            resgenExe = string.Empty;
            var executables = Directory.GetFiles(WINDOWS_SDK_DIRECTORY, RESGEN_EXECUTABLE);
            if (executables.Length == 0)
            {
                _logger.Warn($"Could not find {RESGEN_EXECUTABLE} in {WINDOWS_SDK_DIRECTORY}");
                ResGenFileNotFound?.Invoke(WINDOWS_SDK_DIRECTORY);
                return false;
            }
            if (executables.Length > 1 && !AutoResolveMultipleFiles)
            {
                _logger.Warn($"Found multiple {RESGEN_EXECUTABLE}:\n{string.Join("\n", executables)}");
                FoundMultipleResGenFiles?.Invoke(executables);
                return false;
            }
            resgenExe = executables.Last();
            return true;
        }
    }
}
