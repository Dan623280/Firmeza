using System.ComponentModel.DataAnnotations;
namespace Firmeza.Application.Common;

public sealed class PaginationRequest
{
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [StringLength(150)]
    public string? Search
    {
        get; set;
    }
    public bool IncludeInactive
    {
        get; set;
    }
    public void Validate()
    {
        if (Page < 1 || Page > 100000 || PageSize < 1 || PageSize > 100 || Search?.Length > 150)
            throw new RequestException("validation_failed", "Pagination is invalid.");
    }
}
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
