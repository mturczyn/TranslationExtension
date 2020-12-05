using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddTranslationUINetFramework;

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
