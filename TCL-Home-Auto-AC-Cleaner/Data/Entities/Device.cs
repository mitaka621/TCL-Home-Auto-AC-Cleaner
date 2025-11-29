using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TCL_Home_Auto_AC_Cleaner.Enums;

namespace TCL_Home_Auto_AC_Cleaner.Data.Entities;

[Table("Devices")]
public class Device
{
    [Key]
    [MaxLength(50)]
    public string DeviceId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DeviceName { get; set; }

    [MaxLength(200)]
    public string? NickName { get; set; }

    public OnlineStatusEnum IsOnline { get; set; }

    public int TemperatureType { get; set; }

    [MaxLength(100)]
    public string? Ssid { get; set; }

    [MaxLength(200)]
    public string? LocationName { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<DeviceStatus> DeviceStatuses { get; set; } = new List<DeviceStatus>();

    public virtual ICollection<Cleaning> Cleanings { get; set; } = new List<Cleaning>();
}

