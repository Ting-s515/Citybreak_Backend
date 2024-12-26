using System;
using System.Collections.Generic;

namespace testCitybreak.Models;

public partial class productTable
{
    public int productID { get; set; }

    public string? productName { get; set; }

    public string? prodictIntroduce { get; set; }

    public short? unitPrice { get; set; }

    public byte? unitStock { get; set; }

    public string? imagePath { get; set; }

    public byte? classificationID { get; set; }

    public virtual product_classification? classification { get; set; }

    public virtual ICollection<order_details> order_details { get; set; } = new List<order_details>();
}
