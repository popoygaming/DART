namespace DART.API.Net.DTOs;

public class RecordResponse
{
    public int Id { get; set; }

    public string RecordType { get; set; } = string.Empty;

    public string RegistryNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
