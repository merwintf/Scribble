namespace LibraryDemo.Application.Commands;

public record AddBookCommand(string Title, Guid AuthorId, int Copies);
