using System.Threading.Tasks;

namespace AddTranslation.Core.Abstractions
{
    public interface IProjectItemFactory
    {
        Task<IProjectItem[]> GetProjectItems();
    }
}
