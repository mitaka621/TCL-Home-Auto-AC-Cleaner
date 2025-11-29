using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TCL_Home_Auto_AC_Cleaner.Data.Entities;

[Table("Exceptions")]
public class ExceptionLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Value { get; set; }

    [Required]
    public DateTime TimeOccurred { get; set; }
}

