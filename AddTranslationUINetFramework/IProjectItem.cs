using System;
using System.Collections.Generic;
using System.Text;

namespace AddTranslationUINetFramework
{
    public interface IProjectIem
    {
        string ProjectName { get; }
        void GetProjectDirectory();
    }
}
