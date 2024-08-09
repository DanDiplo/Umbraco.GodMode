using Diplo.GodMode.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Diplo.GodMode.Models
{
    public class RegisteredServiceCollection
    {
        public RegisteredServiceCollection(IServiceCollection services)
        {
            this.Services = new Lazy<List<RegisteredService>>(services.Select(s => new RegisteredService(s)).ToList());
        }

        public Lazy<List<RegisteredService>> Services { get; set; }
    }

    public class RegisteredService
    {
        public RegisteredService(ServiceDescriptor service)
        {
            var serviceType = service.ServiceType;
            var implementationType = service.IsKeyedService
                ? service.KeyedImplementationType
                : service.ImplementationType;

            this.Name = serviceType?.ToGenericTypeString();
            this.FullName = serviceType?.AssemblyQualifiedName;
            this.Namespace = serviceType?.Namespace;
            this.ImplementName = implementationType?.ToGenericTypeString();
            this.ImplementFullName = implementationType?.AssemblyQualifiedName;
            this.ImplementNamespace = implementationType?.Namespace;
            this.Lifetime = service.Lifetime.ToString();
            this.Key = service.IsKeyedService ? service.ServiceKey?.ToString() : string.Empty;
            this.IsPublic = (serviceType?.IsPublic ?? false) && (implementationType?.IsPublic ?? false);
        }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public string FullName { get; set; }

        public bool IsPublic { get; set; }

        public string ImplementName { get; set; }

        public string ImplementNamespace { get; set; }

        public string ImplementFullName { get; set; }

        public string Lifetime { get; set; }

        public string Key { get; set; }
    }
}