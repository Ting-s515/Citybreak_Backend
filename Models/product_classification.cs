using System;
using System.Collections.Generic;

namespace testCitybreak.Models;

public partial class product_classification
{
    public byte classificationID { get; set; }

    public string classification { get; set; } = null!;

    public virtual ICollection<productTable> productTable { get; set; } = new List<productTable>();
}
