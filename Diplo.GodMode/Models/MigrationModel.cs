using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents migration information
    /// </summary>
    public class MigrationModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public string Version { get; set; }

        public string ToDiagnostic()
        {
            return String.Format("{0} - Version: {1}", this.CreateDate, this.Version);
        }
    }
}
