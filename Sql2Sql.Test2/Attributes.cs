using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql.Test
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class IndexAttribute : Attribute
    {
        // This is a positional argument
        public IndexAttribute()
        {
        }
        public IndexAttribute(string name)
        {
        }

        public int Order { get; set; }
        public bool IsUnique { get; set; }
    }
}
