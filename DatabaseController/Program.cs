using DatabaseController.Channels;
using DatabaseController.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=gastos.db"));

builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddSingleton<ObjectQueue>();
builder.Services.AddHostedService<ObjectWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowReact");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    //var frontendPath = Path.Combine(Directory.GetCurrentDirectory(), "ClientApp");

    //if (Directory.Exists(frontendPath))
    //{
    //    var startFront = new ProcessStartInfo
    //    {
    //        FileName = "bash",
    //        Arguments = "-c \"npm run dev\"",
    //        WorkingDirectory = frontendPath,
    //        UseShellExecute = true
    //    };

    //    Process.Start(startFront);
    //}

    await Task.Run(async () =>
    {
        await Task.Delay(3000);

        //Process.Start(new ProcessStartInfo
        //{
        //    FileName = "cmd",
        //    Arguments = "/c start http://localhost:5001",
        //    CreateNoWindow = true
        //});

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = "/c start http://localhost:5000/swagger",
            CreateNoWindow = true
        });
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();