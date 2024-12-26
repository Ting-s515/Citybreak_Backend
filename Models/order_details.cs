using System;
using System.Collections.Generic;

namespace testCitybreak.Models;

public partial class order_details
{
    public short detailID { get; set; }

    public short orderID { get; set; }

    public int productID { get; set; }

    public byte quantity { get; set; }

    public virtual orderTable order { get; set; } = null!;

    public virtual productTable product { get; set; } = null!;
}
