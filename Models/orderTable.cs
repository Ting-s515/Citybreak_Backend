using System;
using System.Collections.Generic;

namespace testCitybreak.Models;

public partial class orderTable
{
    public short orderID { get; set; }

    public int userID { get; set; }

    public string merchantTradeNo { get; set; } = null!;

    public decimal totalPrice { get; set; }

    public string? orderStatus { get; set; }

    public DateTime orderTime { get; set; }

    public DateTime? latestUpdatedTime { get; set; }

    public virtual ICollection<order_details> order_details { get; set; } = new List<order_details>();

    public virtual memberTable user { get; set; } = null!;
}
