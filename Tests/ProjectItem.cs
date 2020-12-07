using AddTranslationUI;
using System;

namespace Tests
{
    class ProjectItem : IProjectIem
    {
        public string ProjectName { get; set; }
        public string FullPathToProjectFile { get; set; }
    }
}
