namespace LibraryDemo.Domain.Entities;

public class Book
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public Guid AuthorId { get; private set; }
    public int CopiesAvailable { get; private set; }

    private Book() { }  // EF Core

    public Book(string title, Guid authorId, int copies)
    {
        Id = Guid.NewGuid();
        Title = title;
        AuthorId = authorId;
        CopiesAvailable = copies;
    }

    public void UpdateTitle(string title) => Title = title;
    public void AddCopies(int count) => CopiesAvailable += count;
    public void RemoveCopy()
    {
        if (CopiesAvailable == 0)
            throw new InvalidOperationException("No copies available.");
        CopiesAvailable--;
    }
    public void ReturnCopy() => CopiesAvailable++;
}
