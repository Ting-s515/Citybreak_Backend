using System;
using System.Collections.Generic;

namespace testCitybreak.Models;

public partial class memberTable
{
    public int userID { get; set; }

    public string? webMemberID { get; set; }

    public string? name { get; set; }

    public string email { get; set; } = null!;

    public string? password { get; set; }

    public string? phone { get; set; }

    public DateOnly createdDate { get; set; }

    public bool loginFromGoogle { get; set; }

    public virtual ICollection<orderTable> orderTable { get; set; } = new List<orderTable>();
}
