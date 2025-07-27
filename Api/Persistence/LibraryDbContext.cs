using LibraryDemo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryDemo.Infrastructure.Persistence;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<BorrowingRecord> BorrowingRecords => Set<BorrowingRecord>();
}
