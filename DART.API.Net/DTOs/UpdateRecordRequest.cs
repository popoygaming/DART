using System.ComponentModel.DataAnnotations;

namespace DART.API.Net.DTOs;

public class UpdateRecordRequest
{
    [Required]
    [MaxLength(20)]
    public string RecordType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RegistryNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
