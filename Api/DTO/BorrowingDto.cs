namespace LibraryDemo.Application.DTOs;

public record BorrowingDto(Guid Id, Guid BookId, Guid MemberId, DateTime BorrowedOn, DateTime? ReturnedOn);
