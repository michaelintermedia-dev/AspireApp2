using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class Userdevice
{
    public int Id { get; set; }

    public int Userid { get; set; }

    public string Devicetoken { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public string? Devicename { get; set; }

    public DateTime? Lastactiveat { get; set; }

    public DateTime? Createdat { get; set; }

    public virtual User User { get; set; } = null!;
}
