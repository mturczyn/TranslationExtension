using AddTranslationUI;
using Microsoft.VisualStudio.Shell;
using System;

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

        public IProjectIem[] GetProjectItems()
        {
            throw new NotImplementedException();
        }
    }
}
