using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class Recording
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime Date { get; set; }
}
