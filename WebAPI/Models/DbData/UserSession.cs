using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class Usersession
{
    public int Id { get; set; }

    public int Userid { get; set; }

    public string Refreshtoken { get; set; } = null!;

    public DateTime Expiresat { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Revokedat { get; set; }

    public string? Deviceinfo { get; set; }

    public virtual User User { get; set; } = null!;
}
