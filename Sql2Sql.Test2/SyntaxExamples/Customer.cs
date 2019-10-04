using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql.Test.SyntaxExamples
{
    /// <summary>
    /// Represents a customer. Used by the syntax examples
    /// </summary>
    public class Customer
    {
        public int Id { get; }
        public string Name { get; }
        public int LocationId { get; }
    }
}
