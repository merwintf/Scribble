namespace LibraryDemo.Domain.Entities;

public class BorrowingRecord
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateTime BorrowedOn { get; private set; }
    public DateTime? ReturnedOn { get; private set; }

    private BorrowingRecord() { }

    public BorrowingRecord(Guid bookId, Guid memberId)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        MemberId = memberId;
        BorrowedOn = DateTime.UtcNow;
    }

    public void ReturnBook() => ReturnedOn = DateTime.UtcNow;
}
