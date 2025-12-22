//using Application.Interface;
//using Infrastructure.DBs;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using System;

//public class LookupService : ILookupService
//{
//    private readonly IMemoryCache _cache;
//    private readonly DBContext _db;

//    public LookupService(IMemoryCache cache, DBContext db)
//    {
//        _cache = cache;
//        _db = db;
//    }

//    public async Task<Dictionary<Guid, string>> GetAssetMapAsync()
//    {
//        return await _cache.GetOrCreateAsync("ASSET_MAP", async entry =>
//        {
//            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
//            return await _db.Assets.ToDictionaryAsync(a => a.AssetId, s => s.Name);
                        
//        });
//    }

//    public async Task<Dictionary<Guid, string>> GetSignalMapAsync()
//    {
//        return await _cache.GetOrCreateAsync("SIGNAL_MAP", async entry =>
//        {
//            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
//            return await _db.Signals
//                .ToDictionaryAsync(s => s.Id, s => s.Name);
//        });
//    }
//}
