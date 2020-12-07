using System;
using System.Collections.Generic;
using System.Text;

namespace AddTranslationUI
{
    public interface IProjectIem
    {
        string ProjectName { get; }
        string FullPathToProjectFile { get; }
    }
}
