using AddTranslationUI;
using System;

namespace Tests
{
    class ProjectItem : IProjectIem
    {
        public string ProjectName { get; set; }

        public void GetProjectDirectory()
        {
            throw new NotImplementedException();
        }
    }
}
