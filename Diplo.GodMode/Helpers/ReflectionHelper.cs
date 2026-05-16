using System.Collections;
using System.Reflection;
using System.Text;
using Diplo.GodMode.Models;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Diplo.GodMode.Helpers
{
    /// <summary>
    /// Helper for dealing with that nasty reflection stuff
    /// This is a bit crazy!
    /// </summary>
    public static class ReflectionHelper
    {
        public static readonly Func<Assembly, bool> IsUmbracoAssemblyPredicate = a => a.ManifestModule.Name.StartsWith("umbraco.", StringComparison.OrdinalIgnoreCase);

        public static readonly Func<Type, Type, bool> IsAssignableClassFromPredicate = (a, b) => a != null && b != null && b.IsClass && !b.IsAbstract && a.IsAssignableFrom(b);

        public static readonly Func<Type, Type, bool> IsAssignableFromPredicate = (a, b) => a.Inherits(b);

        private static readonly string[] PropertiesToIgnore = ["PreviewBadge"];

        public static IEnumerable<Type> GetTypesAssignableFrom(Type baseType, Func<Assembly, bool> predicate = null)
        {
            return GetLoadableTypes(predicate).Where(t => IsAssignableClassFromPredicate(baseType, t));
        }

        public static IEnumerable<Type> GetUmbracoTypesAssignableFrom(Type baseType)
        {
            return GetLoadableTypes(IsUmbracoAssemblyPredicate).Where(t => IsAssignableClassFromPredicate(baseType, t));
        }

        public static IEnumerable<Assembly> GetAssemblies(Func<Assembly, bool> predicate = null)
        {
            return predicate != null ? AppDomain.CurrentDomain.GetAssemblies().Where(ass => !ass.IsDynamic).Where(predicate) : AppDomain.CurrentDomain.GetAssemblies().Where(ass => !ass.IsDynamic);
        }

        public static IEnumerable<Type> GetAllTypesImplementingOpenGenericType(Type openGenericType, IEnumerable<Type> types)
        {
            return from x in types
                   from z in x.GetInterfaces()
                   let y = x.BaseType
                   where
                   (y != null && y.IsGenericType &&
                   openGenericType.IsAssignableFrom(y.GetGenericTypeDefinition())) ||
                   (z.IsGenericType &&
                   openGenericType.IsAssignableFrom(z.GetGenericTypeDefinition()))
                   select x;
        }

        public static IEnumerable<Type> GetAllTypesImplementingOpenGenericType(Type openGenericType) => GetAllTypesImplementingOpenGenericType(openGenericType, GetLoadableTypes());

        public static IEnumerable<Type> GetLoadableTypes(Func<Assembly, bool> assemblyPredicate = null)
        {
            return GetAssemblies(assemblyPredicate).SelectMany(s => GetTypesThatCanBeLoaded(s));
        }

        public static IEnumerable<Assembly> GetUmbracoAssemblies()
        {
            return GetAssemblies(IsUmbracoAssemblyPredicate);
        }

        public static IEnumerable<Type> GetLoadableUmbracoTypes()
        {
            return GetLoadableTypes(IsUmbracoAssemblyPredicate);
        }

        public static IEnumerable<TypeMap> GetTypeMapFrom(Type myType)
        {
            return GetTypesAssignableFrom(myType).Select(t => new TypeMap(t));
        }

        public static IEnumerable<TypeMap> GetInterfaceTypeMapWithImplementationCounts(Assembly assembly)
        {
            var loadableTypes = GetLoadableTypes().ToArray();

            return GetNonGenericInterfaces(assembly)
                .Select(i =>
                {
                    var interfaceType = Type.GetType(i.LoadableName);
                    i.ImplementationCount = interfaceType is null
                        ? 0
                        : loadableTypes.Count(t => IsAssignableClassFromPredicate(interfaceType, t));
                    return i;
                });
        }

        public static TypeDetail? GetTypeDetail(string loadableName)
        {
            var type = Type.GetType(loadableName);
            if (type is null)
            {
                return null;
            }

            var chain = new List<TypeMap>();
            var baseType = type.BaseType;
            while (baseType is not null)
            {
                chain.Add(new TypeMap(baseType));
                baseType = baseType.BaseType;
            }

            return new TypeDetail(type)
            {
                InheritanceChain = chain,
                Interfaces = type.GetInterfaces()
                    .Where(i => i.IsPublic)
                    .OrderBy(i => i.FullName)
                    .Select(i => new TypeMap(i))
            };
        }

        public static IEnumerable<TypeMap> GetTypeMapFromOpenGeneric(Type openGenericType)
        {
            return GetAllTypesImplementingOpenGenericType(openGenericType)
                .Where(t => t != null && t.IsClass && !t.IsAbstract)
                .GroupBy(t => t.GetFullNameWithAssembly())
                .Select(g => g.First())
                .OrderBy(t => t.Name)
                .Select(t => new TypeMap(t));
        }

        public static IEnumerable<TypeMap> GetMiddlewareTypeMap()
        {
            return GetLoadableTypes()
                .Where(IsMiddlewareType)
                .GroupBy(t => t.GetFullNameWithAssembly())
                .Select(g => g.First())
                .OrderBy(t => t.Name)
                .Select(t => new TypeMap(t));
        }

        public static IEnumerable<TypeMap> GetPublishedContentModelTypeMap()
        {
            return GetLoadableTypes()
                .Where(IsPublishedContentModelType)
                .GroupBy(t => t.GetFullNameWithAssembly())
                .Select(g => g.First())
                .OrderBy(t => t.Name)
                .Select(t => new TypeMap(t));
        }

        public static IEnumerable<TypeMap> GetNonGenericInterfaces(Assembly assembly)
        {
            return assembly.GetLoadableTypes().Where(t => t != null && !t.IsGenericType && t.IsPublic && !t.IsGenericTypeDefinition && t.IsInterface).Select(t => new TypeMap(t));
        }

        public static IEnumerable<TypeMap> GetNonGenericTypes(Assembly assembly)
        {
            return assembly.GetLoadableTypes().Where(t => t != null && !t.IsGenericType && t.IsPublic).Select(t => new TypeMap(t));
        }

        public static List<FieldInfo> GetAllPublicConstants(this Type type)
        {
            return type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToList();
        }

        public static IEnumerable<Type> GetTypesInNameSpace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(type => type.Namespace == nameSpace);
        }

        public static IEnumerable<Type> GetInterfaceTypesInNameSpace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(type => type.IsInterface && type.Namespace == nameSpace);
        }

        public static IEnumerable<Type> GetTypesInNameSpace(string nameSpace)
        {
            return GetTypesInNameSpace(Assembly.GetExecutingAssembly(), nameSpace);
        }

        public static IEnumerable<Diagnostic> PopulateDiagnosticsFromConstants(Type type)
        {
            if (type == null)
            {
                return [];
            }

            return type.GetAllPublicConstants().Select(x => new Diagnostic(x.Name, x.GetRawConstantValue().ToString()));
        }

        public static IEnumerable<Diagnostic> PopulateDiagnosticsFrom(object obj, bool onlyUmbraco = true, bool ignoreNestedType = true)
        {
            if (obj == null)
            {
                return [];
            }

            var props = obj.GetType().GetAllProperties();

            if (props == null)
            {
                return [];
            }

            if (onlyUmbraco)
            {
                props = props.Where(x => x.Module.Name.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            var diagnostics = new List<Diagnostic>(props.Length);

            foreach (var prop in props.Where(p => !PropertiesToIgnore.InvariantContains(p.Name)))
            {
                if (!CanReadProperty(prop))
                {
                    continue;
                }

                var displayName = GetPropertyDisplayName(prop);

                if (!TryGetPropertyValue(obj, prop, out var value, out var error))
                {
                    diagnostics.Add(new Diagnostic(displayName, $"Unable to evaluate ({error})"));
                    continue;
                }

                if (value != null)
                {
                    if (value.GetType().IsPublic)
                    {
                        if (prop.PropertyType.IsArray || (prop.PropertyType != typeof(string) && prop.PropertyType.GetInterfaces().Contains(typeof(IEnumerable))))
                        {
                            var items = ((IEnumerable)value)
                                .Cast<object>()
                                .Select(x => x?.ToString())
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToList();

                            string sValue = string.Join(", ", items);

                            diagnostics.Add(new Diagnostic(displayName, sValue));
                        }
                        else
                        {
                            string sValue = value.ToString();

                            if (ignoreNestedType && sValue.StartsWith("Umbraco."))
                            {
                            }
                            else
                            {
                                diagnostics.Add(new Diagnostic(displayName, sValue));
                            }
                        }
                    }
                }
            }

            return diagnostics;
        }

        private static bool CanReadProperty(PropertyInfo prop)
        {
            return prop.GetMethod != null &&
                prop.GetMethod.GetParameters().Length == 0 &&
                prop.GetIndexParameters().Length == 0;
        }

        private static bool TryGetPropertyValue(object obj, PropertyInfo prop, out object value, out string error)
        {
            try
            {
                value = prop.GetValue(obj);
                error = string.Empty;
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                value = null;
                error = ex.InnerException.GetType().Name;
                return false;
            }
            catch (Exception ex)
            {
                value = null;
                error = ex.GetType().Name;
                return false;
            }
        }

        private static IEnumerable<Type> GetTypesThatCanBeLoaded(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            return GetTypesThatCanBeLoaded(assembly);
        }

        private static string GetPropertyDisplayName(PropertyInfo prop)
        {
            return prop.Name.Split('.').Last() + (prop.PropertyType == typeof(bool) ? "?" : string.Empty);
        }

        private static bool IsPublishedContentModelType(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
            {
                return false;
            }

            if (IsAssignableClassFromPredicate(typeof(PublishedContentModel), type))
            {
                return true;
            }

            string nameSpace = type.Namespace ?? string.Empty;
            bool isPublishedModelsNamespace =
                nameSpace.Equals("Umbraco.Cms.Web.Common.PublishedModels", StringComparison.Ordinal) ||
                nameSpace.EndsWith(".PublishedModels", StringComparison.Ordinal);

            return isPublishedModelsNamespace &&
                (typeof(IPublishedContent).IsAssignableFrom(type) || typeof(IPublishedElement).IsAssignableFrom(type));
        }

        private static bool IsMiddlewareType(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
            {
                return false;
            }

            if (typeof(IMiddleware).IsAssignableFrom(type))
            {
                return true;
            }

            return HasRequestDelegateConstructor(type) && HasMiddlewareInvokeMethod(type);
        }

        private static bool HasRequestDelegateConstructor(Type type)
        {
            return type.GetConstructors()
                .Any(ctor => ctor.GetParameters().Any(p => p.ParameterType == typeof(RequestDelegate)));
        }

        private static bool HasMiddlewareInvokeMethod(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Any(method =>
                {
                    if (method.Name != "Invoke" && method.Name != "InvokeAsync")
                    {
                        return false;
                    }

                    var parameters = method.GetParameters();
                    return parameters.Length > 0 && parameters[0].ParameterType == typeof(HttpContext);
                });
        }

        private static string SplitOnCapitals(string text)
        {
            StringBuilder builder = new StringBuilder();

            foreach (char c in text)
            {
                if (Char.IsUpper(c) && builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
