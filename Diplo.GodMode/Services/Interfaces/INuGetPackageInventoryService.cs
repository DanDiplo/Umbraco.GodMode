using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces;

public interface INuGetPackageInventoryService
{
    NuGetPackageInventory GetInventory();
}
