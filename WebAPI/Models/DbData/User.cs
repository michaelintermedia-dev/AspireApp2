using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public string Passwordsalt { get; set; } = null!;

    public string? Firstname { get; set; }

    public string? Lastname { get; set; }

    public bool? Isemailverified { get; set; }

    public string? Emailverificationtoken { get; set; }

    public string? Passwordresettoken { get; set; }

    public DateTime? Passwordresettokenexpiry { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public DateTime? Lastloginat { get; set; }

    public virtual ICollection<Userdevice> Userdevices { get; set; } = new List<Userdevice>();

    public virtual ICollection<Usersession> Usersessions { get; set; } = new List<Usersession>();
}
