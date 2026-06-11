using System.Reflection;
using System.Runtime.InteropServices;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.DependencyModel;

namespace Diplo.GodMode.Services;

public sealed class NuGetPackageInventoryService : INuGetPackageInventoryService
{
    public NuGetPackageInventory GetInventory()
    {
        var context = DependencyContext.Default;

        if (context is null)
        {
            return new NuGetPackageInventory();
        }

        var libraries = context.RuntimeLibraries;
        var packages = libraries
            .Where(library => library.Type.Equals("package", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(library => library.Name, StringComparer.OrdinalIgnoreCase);
        var projectLibraries = libraries
            .Where(library => library.Type.Equals("project", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var requestedBy = BuildRequestedBy(libraries);
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(ToLoadedAssembly)
            .Where(assembly => !string.IsNullOrWhiteSpace(assembly.Name))
            .ToList();
        var loadedAssemblyNames = loadedAssemblies
            .Select(assembly => assembly.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = packages.Values
            .Select(package =>
            {
                var runtimeAssemblies = GetRuntimeAssemblies(package).ToList();
                var packageLoadedAssemblies = loadedAssemblies
                    .Where(assembly => runtimeAssemblies.Any(runtimeAssembly => AssemblyNameMatches(runtimeAssembly, assembly.Name)) || AssemblyNameMatches(package.Name, assembly.Name))
                    .Select(assembly => string.IsNullOrWhiteSpace(assembly.Location) ? assembly.Name : $"{assembly.Name} ({assembly.Location})")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
                var directProjects = projectLibraries
                    .Where(project => project.Dependencies.Any(dependency => dependency.Name.Equals(package.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(project => project.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
                var incoming = requestedBy.TryGetValue(package.Name, out var requesters)
                    ? requesters.OrderBy(x => x).ToList()
                    : [];
                var direct = directProjects.Count > 0;

                return new NuGetPackageInfo
                {
                    Id = package.Name,
                    Version = package.Version,
                    Type = package.Type,
                    IsDirect = direct,
                    IsTransitive = !direct,
                    IsLoaded = packageLoadedAssemblies.Count > 0 || loadedAssemblyNames.Contains(package.Name),
                    NuGetUrl = $"https://www.nuget.org/packages/{Uri.EscapeDataString(package.Name)}/{Uri.EscapeDataString(package.Version)}",
                    DirectProjects = directProjects,
                    RequestedBy = incoming,
                    Dependencies = package.Dependencies.Select(dependency => $"{dependency.Name} {dependency.Version}").OrderBy(x => x).ToList(),
                    RuntimeAssemblies = runtimeAssemblies,
                    LoadedAssemblies = packageLoadedAssemblies
                };
            })
            .OrderBy(package => package.Id)
            .ToList();

        return new NuGetPackageInventory
        {
            RuntimeName = RuntimeInformation.RuntimeIdentifier,
            TargetFramework = context.Target.Framework,
            PackageCount = items.Count,
            DirectPackageCount = items.Count(item => item.IsDirect),
            LoadedPackageCount = items.Count(item => item.IsLoaded),
            Packages = items
        };
    }

    private static Dictionary<string, HashSet<string>> BuildRequestedBy(IReadOnlyList<RuntimeLibrary> libraries)
    {
        var requestedBy = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var library in libraries)
        {
            foreach (var dependency in library.Dependencies)
            {
                if (!requestedBy.TryGetValue(dependency.Name, out var requesters))
                {
                    requesters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    requestedBy[dependency.Name] = requesters;
                }

                requesters.Add(library.Name);
            }
        }

        return requestedBy;
    }

    private static IEnumerable<string> GetRuntimeAssemblies(RuntimeLibrary package)
        => package.RuntimeAssemblyGroups
            .SelectMany(group => group.AssetPaths)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path);

    private static LoadedAssembly ToLoadedAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? string.Empty;
        string location;

        try
        {
            location = assembly.Location;
        }
        catch (NotSupportedException)
        {
            location = string.Empty;
        }

        return new LoadedAssembly(name, location);
    }

    private static bool AssemblyNameMatches(string runtimeAssemblyPath, string assemblyName)
    {
        var fileName = Path.GetFileNameWithoutExtension(runtimeAssemblyPath);
        return fileName.Equals(assemblyName, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record LoadedAssembly(string Name, string Location);
}
