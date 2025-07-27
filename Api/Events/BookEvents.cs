namespace LibraryDemo.Domain.Events;

public record BookAdded(Guid BookId, string Title, int Copies);
public record BookUpdated(Guid BookId, string Title);
public record BookDeleted(Guid BookId);
