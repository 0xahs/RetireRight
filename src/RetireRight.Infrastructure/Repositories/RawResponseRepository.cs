using RetireRight.Core.Interfaces;
using RetireRight.Core.Models;
using RetireRight.Infrastructure.Database;

namespace RetireRight.Infrastructure.Repositories;

public class RawResponseRepository : IRawResponseRepository
{
    private readonly RetireRightDbContext _db;

    public RawResponseRepository(RetireRightDbContext db) => _db = db;

    public async Task SaveAsync(RawApiResponse response, CancellationToken ct = default)
    {
        response.ScrapedAt = DateTime.UtcNow;
        _db.RawResponses.Add(response);
        await _db.SaveChangesAsync(ct);
    }
}
