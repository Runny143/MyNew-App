using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("task/(.*)", "todos/$1"));


app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: [{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started");
    await next();
    Console.WriteLine($"Response: [{context.Response.StatusCode} {DateTime.UtcNow}] Finished");
});


var todos = new List<Todo>();

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var todo = todos.SingleOrDefault(t => id == t.Id);
    return todo is not null
    ? TypedResults.Ok(todo)
    : TypedResults.NotFound();
});

app.MapGet("/todos", () => todos);

app.MapPost("/todos", (Todo task) =>
{
    todos.Add(task);
    // In a real application, you would save the todo item to a database here.
    return TypedResults.Created("/todos/{Id}", task);
})
.AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
   if (taskArgument.Duedate < DateTime.UtcNow)
    {
        errors.Add(nameof(taskArgument.Duedate), new[] { "Duedate must be in the future." });
    }
    if (taskArgument.IsCompleted)
    {
        errors.Add(
            nameof(Todo.IsCompleted), new[] { "New tasks cannot be added." }
        );
    }
    if (errors.Count > 0)
    {
        return TypedResults.ValidationProblem(errors);
    }
    return await next(context);
});


app.MapPatch("/todos/{id}/complete", Results<Ok<Todo>, NotFound> (int id) =>
{
    var todoIndex = todos.FindIndex(t => t.Id == id);
    if (todoIndex == -1)
    {
        return TypedResults.NotFound();
    }
    
    var todo = todos[todoIndex];
    var completedTodo = todo with { IsCompleted = true };
    todos[todoIndex] = completedTodo;
    
    return TypedResults.Ok(completedTodo);
});

app.MapDelete("/todos/{id}", (int id) =>
{
    todos.RemoveAll(t => t.Id == id);
    return TypedResults.NoContent();
});


app.Run();

public record Todo(int Id, string Title, DateTime Duedate, bool IsCompleted);

interface ITaskService
{
    Todo? GetTodoById(int id);

    List<Todo> GetAllTodos();
    void AddTodo(Todo todo);
    void RemoveTodo(int id);
}

class inmemorytaskservice : ITaskService
{
    private readonly List<Todo> _todos = [];

    public Todo? GetTodoById(int id) => _todos.SingleOrDefault(t => t.Id == id);

    public List<Todo> GetAllTodos() => _todos;

    public void AddTodo(Todo task) => _todos.Add(_todos);

    public void RemoveTodo(int id) => _todos.RemoveAll(t => t.Id == id);
}
