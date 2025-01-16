using System;
using System.Collections.Generic;

namespace testCitybreak.Models;

public partial class product_categories
{
    public short categoriesID { get; set; }

    public string categories { get; set; } = null!;

    public virtual ICollection<productTable> productTable { get; set; } = new List<productTable>();
}
