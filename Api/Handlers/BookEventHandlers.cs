using LibraryDemo.Application.DTOs;
using LibraryDemo.Domain.Events;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryDemo.Application.Handlers;

public class BookEventHandlers
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public BookEventHandlers(IMemoryCache cache) => _cache = cache;

    public void Handle(BookAdded evt)
    {
        var dto = new BookDto(evt.BookId, evt.Title, authorId: default, evt.Copies);
        _cache.Set($"Book:{evt.BookId}", dto, CacheTtl);
    }

    public void Handle(BookUpdated evt)
    {
        if (_cache.TryGetValue($"Book:{evt.BookId}", out BookDto old))
        {
            var updated = old with { Title = evt.Title };
            _cache.Set($"Book:{evt.BookId}", updated, CacheTtl);
        }
    }

    public void Handle(BookDeleted evt)
    {
        _cache.Remove($"Book:{evt.BookId}");
    }
}
