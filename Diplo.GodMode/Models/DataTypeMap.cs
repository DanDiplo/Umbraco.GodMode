using System;

namespace Diplo.GodMode.Models
{
    public class DataTypeMap : ItemBase
    {
        public string DbType { get; set; }

        public bool IsUsed { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}