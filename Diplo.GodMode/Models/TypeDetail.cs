using System.Collections.Generic;

namespace Diplo.GodMode.Models
{
    public class TypeDetail : TypeMap
    {
        public TypeDetail(System.Type type) : base(type)
        {
        }

        public IEnumerable<TypeMap> InheritanceChain { get; set; } = [];

        public IEnumerable<TypeMap> Interfaces { get; set; } = [];
    }
}
