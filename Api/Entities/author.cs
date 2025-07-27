namespace LibraryDemo.Domain.Entities;

public class Author
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private Author() { }
    public Author(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}
