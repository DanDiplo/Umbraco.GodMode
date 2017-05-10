using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents registered server information
    /// </summary>
    public class ServerModel
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string ComputerName { get; set; }

        public DateTime RegisteredDate { get; set; }

        public DateTime LastNotifiedDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsMaster { get; set; }

        public string ToDiagnostic()
        {
            return String.Format("{0}{1} - Registered: {2}, Modified: {3}, Active: {4}, Master: {5}", this.IsMaster ? "* " : String.Empty, this.Address, this.RegisteredDate, this.LastNotifiedDate, this.IsActive, this.IsMaster);
        }
    }
}
