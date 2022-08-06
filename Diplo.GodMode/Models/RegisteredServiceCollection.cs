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
            this.Name = service.ServiceType?.ToGenericTypeString();
            this.Namespace = service.ServiceType?.Namespace;
            this.FullName = service.ServiceType?.AssemblyQualifiedName;
            this.ImplementFullName = service.ImplementationType?.AssemblyQualifiedName;
            this.ImplementName = service.ImplementationType?.ToGenericTypeString();
            this.ImplementNamespace = service.ImplementationType?.Namespace;
            this.Lifetime = service.Lifetime.ToString();
            this.IsPublic = (service.ServiceType?.IsPublic ?? false) && (service.ImplementationType?.IsPublic ?? false);
        }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public string FullName { get; set; }

        public bool IsPublic { get; set; }

        public string ImplementName { get; set; }

        public string ImplementNamespace { get; set; }

        public string ImplementFullName { get; set; }

        public string Lifetime { get; set; }
    }
}
