using System.Threading.Tasks;
using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services
{
    public interface IUtilitiesService
    {
        Task<ServerResponse> ClearMediaFileCacheAsync();

        ServerResponse ClearUmbracoCacheFor(string cache);
    }
}