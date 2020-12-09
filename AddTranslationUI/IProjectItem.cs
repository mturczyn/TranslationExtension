namespace AddTranslationUI
{
    public interface IProjectIem
    {
        string ProjectName { get; }
        string FullPathToProjectFile { get; }
        /// <summary>
        /// Checks if project directory contains correctly placed resources files as well as designer file.
        /// </summary>
        /// <returns></returns>
        bool CheckIfIsCorrectResourcesProject();
    }
}
