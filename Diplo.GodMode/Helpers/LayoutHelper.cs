using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Diplo.GodMode.Helpers
{
    public class LayoutHelper
    {
        /// <summary>
        /// Reguar expression to find partials in the template text. Adds a group for cached partials.
        /// </summary>
        private static readonly Regex LayoutRegex = new Regex(@"@{(.*?)Layout(\s*)=(\s*)""(.+).cshtml"";(.*?)}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }
}
