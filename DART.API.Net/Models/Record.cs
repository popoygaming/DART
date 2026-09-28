namespace DART.API.Net.Models;

public class Record
{
    public int Id { get; set; }

    public RecordType RecordType { get; set; }

    public string RegistryNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User? CreatedByUser { get; set; }
}
