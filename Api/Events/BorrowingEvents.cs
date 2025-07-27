namespace LibraryDemo.Domain.Events;

public record BookBorrowed(Guid BookId, Guid MemberId);
public record BookReturned(Guid BookId, Guid MemberId);
