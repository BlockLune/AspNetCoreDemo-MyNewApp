using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService()); // if i want to use database, i can create something like SqlTaskService and register it here.

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) =>
{
  Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
  await next(context); // Call the next. Middlewares are executed nestedly.
  Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});

var todos = new List<Todo>();

app.MapGet("/todos", (ITaskService service) => service.GetTodos()); // Not use TypedResults<T> here. The minimal API will recognize it and serialize it to JSON automatically.
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
  var targetTodo = service.GetTodoById(id);
  return targetTodo is null
    ? TypedResults.NotFound()
    : TypedResults.Ok(targetTodo);
});
app.MapPost("/todos", (Todo task, ITaskService service) =>
{
  service.AddTodo(task);
  return TypedResults.Created("/todos/{id}", task);
}).AddEndpointFilter(async (context, next) =>
{
  var taskArgument = context.GetArgument<Todo>(0); // zero-based index
  var errors = new Dictionary<string, string[]>(); // key: property name, value: error messages
  if (taskArgument.DueDate < DateTime.UtcNow)
  {
    errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past."]);
  }
  if (taskArgument.IsCompleted)
  {
    errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo."]);
  }
  if (errors.Count > 0)
  {
    return Results.ValidationProblem(errors); // http code 400
  }
  return await next(context);
});
app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
  service.DeleteTodoById(id);
  return TypedResults.NoContent(); // http code 204
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

interface ITaskService
{
  Todo? GetTodoById(int id);
  List<Todo> GetTodos();
  void DeleteTodoById(int id);
  Todo AddTodo(Todo task);
}

class InMemoryTaskService : ITaskService
{
  private readonly List<Todo> _todos = [];

  public Todo AddTodo(Todo task)
  {
    _todos.Add(task);
    return task;
  }

  public void DeleteTodoById(int id)
  {
    _todos.RemoveAll(task => id == task.Id);
  }

  public Todo? GetTodoById(int id)
  {
    return _todos.SingleOrDefault(t => id == t.Id);
  }

  public List<Todo> GetTodos()
  {
    return _todos;
  }
}