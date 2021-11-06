using AddTranslationCore.Abstractions;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AddTranslation
{
    /// <summary>
    /// Class is used to provide project items representing project references of given project.
    /// In order to provide these references we use object of class <see cref="AsyncPackage"/>.
    /// </summary>
    class ProjectItemsFactory : IProjectItemFactory
    {
        private AsyncPackage _serviceProvider;

        public ProjectItemsFactory(AsyncPackage serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<IProjectItem[]> GetProjectItems()
        {
            var dte = (EnvDTE80.DTE2)await _serviceProvider.GetServiceAsync(typeof(DTE));
            Assumes.Present(dte);
            var vsProject = dte.ActiveDocument.ProjectItem.ContainingProject.Object as VSLangProj.VSProject;
            if (vsProject == null)
            {
                // Cant get the current project.
                return null;
            }
            var projectReferences = new List<AddTranslationCore.ViewModel.Project>();
            foreach (Project item in dte.Solution.Projects)
            {
                projectReferences.Add(new AddTranslationCore.ViewModel.Project(item.FullName, item.Name));
            }

            return projectReferences.ToArray();
        }
    }
}
