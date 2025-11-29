using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TCL_Home_Auto_AC_Cleaner.Data.Entities;

[Table("DeviceStatuses")]
public class DeviceStatus
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [ForeignKey(nameof(Device))]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public DateTime TimeStatusExtracted { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? StatusProperties { get; set; }

    public virtual Device Device { get; set; } = null!;
}

