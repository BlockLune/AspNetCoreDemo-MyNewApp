using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>();

app.MapGet("/todos", () => todos); // Not use TypedResults<T> here. The minimal API will recognize it and serialize it to JSON automatically.
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
  var targetTodo = todos.SingleOrDefault(t => id == t.Id);
  return targetTodo is null
    ? TypedResults.NotFound()
    : TypedResults.Ok(targetTodo);
});
app.MapPost("/todos", (Todo task) =>
{
  todos.Add(task);
  return TypedResults.Created("/todos/{id}", task);
});
app.MapDelete("/todos/{id}", (int id) =>
{
  todos.RemoveAll(t => t.Id == id);
  return TypedResults.NoContent(); // http code 204
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);
