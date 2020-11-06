using System.Threading.Tasks;

namespace HelloWorld.Interfaces
{
    public interface ILimit : Orleans.IGrainWithStringKey
    {
         Task<bool> checkLimitOk();
    }
}