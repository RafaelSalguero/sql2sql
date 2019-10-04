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
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public int LocationId { get; set; }
    }

    /// <summary>
    /// Represents a customer with extra columns. Used by the syntax examples
    /// </summary>
    public class CustomerDto : Customer
    {
        public string FullName { get; set; }
    }
}
