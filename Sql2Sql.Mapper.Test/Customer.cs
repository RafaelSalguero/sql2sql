using System;
using System.Collections.Generic;
using System.Text;

namespace Sql2Sql.Mapper.Test
{
    public class Customer1
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
    }

    public class Customer2
    {
        public Customer2(int id, string name, CustomerType? type, CustomerType? type2, Address dir1)
        {
            Id = id;
            Name = name;
            Type = type;
            Dir1 = dir1;
            Type2 = type2;
        }

        public int Id { get; }
        public string Name { get; }
        public CustomerType? Type { get;  }
        public CustomerType? Type2 { get; }
        public Address Dir1 { get;  }
        public Address Dir2 { get; set; }
        public DateTime Date { get; set; }
        public string NullStr { get; set; }
    }
    
    public enum CustomerType
    {
        Admin,
        Other
    }

    public class Address
    {
        public string Street { get; set; }
        public PersonalData Personal { get; set; }
    }

    public class PersonalData
    {
        public PersonalData(string phone)
        {
            Phone = phone;
        }

        public string Phone { get;  }
        public string Cellphone { get; set; }
    }
}
