using Wolverine.Attributes;
using LibraryDemo.Application.Commands;
using LibraryDemo.Domain.Entities;
using LibraryDemo.Domain.Events;
using LibraryDemo.Infrastructure.Persistence;

namespace LibraryDemo.Application.Handlers;

public class BookCommandHandlers
{
    private readonly LibraryDbContext _db;
    public BookCommandHandlers(LibraryDbContext db) => _db = db;

    [Transactional]
    public async Task<BookAdded> Handle(AddBookCommand cmd)
    {
        var book = new Book(cmd.Title, cmd.AuthorId, cmd.Copies);
        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return new BookAdded(book.Id, book.Title, cmd.Copies);
    }

    [Transactional]
    public async Task<BookUpdated> Handle(UpdateBookCommand cmd)
    {
        var book = await _db.Books.FindAsync(cmd.Id)
                   ?? throw new KeyNotFoundException("Book not found");
        book.UpdateTitle(cmd.Title);
        await _db.SaveChangesAsync();
        return new BookUpdated(book.Id, cmd.Title);
    }

    [Transactional]
    public async Task<BookDeleted> Handle(DeleteBookCommand cmd)
    {
        var book = await _db.Books.FindAsync(cmd.Id)
                   ?? throw new KeyNotFoundException("Book not found");
        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        return new BookDeleted(cmd.Id);
    }
}
