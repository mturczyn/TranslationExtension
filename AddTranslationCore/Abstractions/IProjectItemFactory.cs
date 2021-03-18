using System.Threading.Tasks;

namespace AddTranslationCore.Abstractions
{
    public interface IProjectItemFactory
    {
        Task<IProjectItem[]> GetProjectItems();
    }
}
