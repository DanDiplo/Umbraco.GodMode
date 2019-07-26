using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents an Umbraco member
    /// </summary>
    public class MemberModel
    {
        /// <summary>
        /// Member Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Member username / login name
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Member name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Member email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Date member created
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Member UDI
        /// </summary>
        public Guid Udi { get; set; }
    }
}
