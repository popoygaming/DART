using System.Security.Claims;
using DART.API.Net.Data;
using DART.API.Net.DTOs;
using DART.API.Net.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DART.API.Net.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecordsController : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly DartDbContext _dbContext;

    public RecordsController(DartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateRecordRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request.RecordType, request.RegistryNumber, request.Name, out var parsedRecordType, out var normalizedRegistryNumber, out var normalizedName, out var validationMessage))
        {
            return BadRequest(new { message = validationMessage });
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var entity = new Record
        {
            RecordType = parsedRecordType,
            RegistryNumber = normalizedRegistryNumber,
            Name = normalizedName,
            CreatedBy = currentUserId.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Records.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/records/{entity.Id}", ToResponse(entity));
    }

    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Records
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return NotFound(new { message = "Record not found." });
        }

        return Ok(ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecordRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request.RecordType, request.RegistryNumber, request.Name, out var parsedRecordType, out var normalizedRegistryNumber, out var normalizedName, out var validationMessage))
        {
            return BadRequest(new { message = validationMessage });
        }

        var entity = await _dbContext.Records.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "Record not found." });
        }

        entity.RecordType = parsedRecordType;
        entity.RegistryNumber = normalizedRegistryNumber;
        entity.Name = normalizedName;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(entity));
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PagedRecordsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? registryNumber,
        [FromQuery] string? recordType,
        [FromQuery] int page = DefaultPage,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0)
        {
            return BadRequest(new { message = "Page must be greater than 0." });
        }

        if (pageSize <= 0 || pageSize > MaxPageSize)
        {
            return BadRequest(new { message = $"PageSize must be between 1 and {MaxPageSize}." });
        }

        RecordType? parsedRecordType = null;
        if (!string.IsNullOrWhiteSpace(recordType))
        {
            if (!TryParseRecordType(recordType, out var parsed))
            {
                return BadRequest(new { message = "RecordType must be one of: Birth, Marriage, Death." });
            }

            parsedRecordType = parsed;
        }

        var normalizedName = name?.Trim();
        var normalizedRegistryNumber = registryNumber?.Trim();

        var query = _dbContext.Records.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedName))
        {
            query = query.Where(r => EF.Functions.Like(r.Name, $"%{normalizedName}%"));
        }

        if (!string.IsNullOrWhiteSpace(normalizedRegistryNumber))
        {
            query = query.Where(r => r.RegistryNumber == normalizedRegistryNumber);
        }

        if (parsedRecordType.HasValue)
        {
            query = query.Where(r => r.RecordType == parsedRecordType.Value);
        }

        query = query.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RecordResponse
            {
                Id = r.Id,
                RecordType = r.RecordType.ToString(),
                RegistryNumber = r.RegistryNumber,
                Name = r.Name,
                CreatedBy = r.CreatedBy,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var response = new PagedRecordsResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    private int? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsed) ? parsed : null;
    }

    private static bool TryValidateRequest(
        string recordType,
        string registryNumber,
        string name,
        out RecordType parsedRecordType,
        out string normalizedRegistryNumber,
        out string normalizedName,
        out string errorMessage)
    {
        parsedRecordType = default;
        recordType ??= string.Empty;
        registryNumber ??= string.Empty;
        name ??= string.Empty;

        normalizedRegistryNumber = registryNumber.Trim();
        normalizedName = name.Trim();
        errorMessage = string.Empty;

        if (!TryParseRecordType(recordType, out parsedRecordType))
        {
            errorMessage = "RecordType must be one of: Birth, Marriage, Death.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(normalizedRegistryNumber))
        {
            errorMessage = "RegistryNumber is required.";
            return false;
        }

        if (normalizedRegistryNumber.Length > 50)
        {
            errorMessage = "RegistryNumber must not exceed 50 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errorMessage = "Name is required.";
            return false;
        }

        if (normalizedName.Length > 200)
        {
            errorMessage = "Name must not exceed 200 characters.";
            return false;
        }

        return true;
    }

    private static bool TryParseRecordType(string value, out RecordType recordType)
    {
        return Enum.TryParse(value?.Trim(), ignoreCase: true, out recordType)
            && Enum.IsDefined(recordType);
    }

    private static RecordResponse ToResponse(Record entity)
    {
        return new RecordResponse
        {
            Id = entity.Id,
            RecordType = entity.RecordType.ToString(),
            RegistryNumber = entity.RegistryNumber,
            Name = entity.Name,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
