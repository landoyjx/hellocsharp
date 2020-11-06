using System.Threading.Tasks;

namespace HelloWorld.Interfaces
{
    /// <summary>
    /// Orleans grain communication interface IValue
    /// </summary>
    public interface IValue : Orleans.IGrainWithStringKey
    {
        Task<string> GetAsync();

        void SetAsync(string value);
    }
}
