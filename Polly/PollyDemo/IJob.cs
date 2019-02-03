using System.Threading.Tasks;

namespace PollyDemo
{
    public interface IJob
    {
        bool DoWork();

        Task<bool> DoWorkAsync();
    }
}