using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UserDb>(opt => opt.UseInMemoryDatabase("UserDb"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "UserAPI";
    config.Title = "UserAPI v1";
    config.Version = "v1";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "UserAPI Swagger UI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

var userItems = app.MapGroup("/users");

userItems.MapGet("/", GetAllUsers);
userItems.MapGet("/{id}", GetUser);
userItems.MapPost("/", CreateUser);
userItems.MapPut("/{id}", UpdateUser);
userItems.MapDelete("/{id}", DeleteUser);

app.Run();

static async Task<IResult> GetAllUsers(UserDb db)
{
    return TypedResults.Ok(await db.Users.ToArrayAsync());
}

static async Task<IResult> GetUser(int id, UserDb db)
{
    return await db.Users.FindAsync(id)
        is User user
            ? TypedResults.Ok(user)
            : TypedResults.NotFound();
}

static async Task<IResult> CreateUser(User user, UserDb db)
{
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/users/{user.Id}", user);
}

static async Task<IResult> UpdateUser(int id, User inputUser, UserDb db)
{
    var user = await db.Users.FindAsync(id);

    if (user is null) return TypedResults.NotFound();

    user.Name = inputUser.Name;
    user.Email = inputUser.Email;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteUser(int id, UserDb db)
{
    if (await db.Users.FindAsync(id) is User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}