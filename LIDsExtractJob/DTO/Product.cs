using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIDsExtractJob
{
    public class Product 
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }

        public  string ImageUrl { get; set; }

        public decimal Price { get; set; }    
        public decimal OldPrice { get; set; }
        public string DetailPath { get; set; }
        public DateTime? CreatedDate { get; set;        }

        public DateTime? ModifiedDate { get; set; }

        public DateTime? DeletedDate { get; set; }

       
    }

    public class ProductComparer : IEqualityComparer<Product>
    {
        public bool Equals(Product x, Product y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(Product obj)
        {
            return obj.Id.GetHashCode() * 17;
        }
    }
}
