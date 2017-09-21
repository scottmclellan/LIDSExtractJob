using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIDsExtractJob
{
    public class Item 
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public  string ImageUrl { get; set; }

        public decimal Price { get; set; }    
        public decimal OldPrice { get; set; }
        public string DetailPath { get; set; }
        public DateTime? CreatedDate { get; set;        }

        public DateTime? ModifiedDate { get; set; }

        public DateTime? DeletedDate { get; set; }

       
    }

    public class ItemComparer : IEqualityComparer<Item>
    {
        public bool Equals(Item x, Item y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(Item obj)
        {
            return obj.Id.GetHashCode() * 17;
        }
    }
}
