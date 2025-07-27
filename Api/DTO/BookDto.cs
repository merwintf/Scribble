namespace LibraryDemo.Application.DTOs;

public record BookDto(Guid Id, string Title, Guid AuthorId, int CopiesAvailable);
