using LibraryDemo.Application.Commands;
using LibraryDemo.Application.DTOs;
using LibraryDemo.Application.Queries;
using LibraryDemo.Domain.Events;
using Wolverine;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IMessageBus _bus;

    public BooksController(IMessageBus bus) => _bus = bus;

    [HttpPost]
    public async Task<IActionResult> Add(AddBookCommand cmd)
    {
        var @event = await _bus.InvokeAsync<BookAdded>(cmd);
        return CreatedAtAction(nameof(GetById), new { id = @event.BookId }, null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateBookCommand cmd)
    {
        await _bus.InvokeAsync(cmd with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _bus.InvokeAsync(new DeleteBookCommand(id));
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto?>> GetById(Guid id)
    {
        var dto = await _bus.InvokeAsync(new GetBookByIdQuery(id));
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<BookDto>>> Search([FromQuery] string? title, [FromQuery] string? author)
    {
        var dtos = await _bus.InvokeAsync(new SearchBooksQuery(title, author));
        return Ok(dtos);
    }
}
