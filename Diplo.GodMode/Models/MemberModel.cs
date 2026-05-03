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
        /// Member type node Id
        /// </summary>
        public int MemberTypeId { get; set; }

        /// <summary>
        /// Member type name
        /// </summary>
        public string MemberTypeName { get; set; }

        /// <summary>
        /// Member type alias
        /// </summary>
        public string MemberTypeAlias { get; set; }

        /// <summary>
        /// Comma-separated member group names
        /// </summary>
        public string Groups { get; set; }

        /// <summary>
        /// Whether the member is approved
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Whether the member is locked out
        /// </summary>
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// Whether the member has any two-factor login providers configured
        /// </summary>
        public bool UsesTwoFactor { get; set; }

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
