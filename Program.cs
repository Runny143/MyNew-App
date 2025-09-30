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
});

app.MapDelete("/todos/{id}", (int id) =>
{
    todos.RemoveAll(t => t.Id == id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Title, DateTime Duedate, bool IsCompleted);
