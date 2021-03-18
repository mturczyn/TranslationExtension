using AddTranslationCore.Abstractions;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
            var projects1 = dte.Solution.Projects as Projects;
            foreach(var item in projects1)
            {

            }
            var solution = dte.Solution;
            foreach(var item in solution)
            {

            }

            var sol = await _serviceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            uint numProjects;
            ErrorHandler.ThrowOnFailure(
                sol.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, 0, null, out numProjects));
            string[] projects = new string[numProjects];
            ErrorHandler.ThrowOnFailure(
                sol.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, numProjects, projects, out numProjects));
            // GetProjectFilesInSolution also returns solution folders, so we want to do some filtering
            // things that don't exist on disk certainly can't be project files
            //return projects.Where(p => !string.IsNullOrEmpty(p) && System.IO.File.Exists(p)).ToArray();

            var projectReferences = new List<IProjectItem>();
            foreach (var @ref in projects)
            {
                var p = new AddTranslationCore.ViewModel.Project(@ref, "");
            }

            return projectReferences.ToArray();
        }
    }
}
