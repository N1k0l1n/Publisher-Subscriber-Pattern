using MessageBroker.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


//Database Connection
builder.Services.AddDbContext<AppDbContext>(options =>
{

    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection"));
});


var app = builder.Build();



app.UseHttpsRedirection();



app.Run();
