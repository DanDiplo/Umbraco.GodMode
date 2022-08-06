using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Helpers
{
    public static class TypeExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        /// <remarks>
        /// Borrwed from https://stackoverflow.com/a/2448918/140392
        /// </remarks>
        public static string ToGenericTypeString(this Type t)
        {
            if (!t.IsGenericType)
            {
                return t.Name;
            }

            string genericTypeName = t.GetGenericTypeDefinition().Name;

            var pos = genericTypeName.IndexOf('`');

            if (pos < 1)
            {
                pos = 0;
            }

            genericTypeName = genericTypeName.Substring(0, pos);

            string genericArgs = string.Join(", ", t.GetGenericArguments().Select(ta => ToGenericTypeString(ta)).ToArray());

            return genericTypeName + "<" + genericArgs + ">";
        }

    }
}
