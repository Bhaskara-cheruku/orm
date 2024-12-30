using Custom_ORM.Data;

var builder = WebApplication.CreateBuilder(args);

// Register MyCustomDbContext
builder.Services.AddScoped<MyCustomDbContext>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection"); // Assuming it's in appsettings.json
    return new MyCustomDbContext(connectionString);
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Create a scope to resolve scoped services
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MyCustomDbContext>();
    dbContext.EnsureTablesCreated();  // Ensure tables are created using MyCustomDbContext
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
