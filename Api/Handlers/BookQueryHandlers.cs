using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LibraryDemo.Application.DTOs;
using LibraryDemo.Application.Queries;
using LibraryDemo.Infrastructure.Persistence;

namespace LibraryDemo.Application.Handlers;

public class BookQueryHandlers
{
    private readonly LibraryDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public BookQueryHandlers(LibraryDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<BookDto?> Handle(GetBookByIdQuery query)
    {
        var key = $"Book:{query.Id}";
        if (_cache.TryGetValue(key, out BookDto dto))
            return dto;

        dto = await _db.Books
            .Where(b => b.Id == query.Id)
            .Select(b => new BookDto(b.Id, b.Title, b.AuthorId, b.CopiesAvailable))
            .FirstOrDefaultAsync();

        if (dto is not null)
            _cache.Set(key, dto, CacheTtl);

        return dto;
    }

    public async Task<List<BookDto>> Handle(SearchBooksQuery query)
    {
        var key = $"BookSearch:{query.Title}:{query.AuthorName}";
        if (_cache.TryGetValue(key, out List<BookDto> list))
            return list;

        var q = _db.Books.Include(b => b.Author).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Title))
            q = q.Where(b => b.Title.Contains(query.Title));
        if (!string.IsNullOrWhiteSpace(query.AuthorName))
            q = q.Where(b => b.Author.Name.Contains(query.AuthorName));

        list = await q
            .Select(b => new BookDto(b.Id, b.Title, b.AuthorId, b.CopiesAvailable))
            .ToListAsync();

        _cache.Set(key, list, CacheTtl);
        return list;
    }
}
