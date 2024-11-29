using DefenseApiExample.Services;
using LiteDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IDefenseService, DefenseService>();
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<LiteDatabase>(_ => new LiteDatabase("DefenseDb.db"));
builder.Services.AddSingleton<TcpServerService>();
builder.Services.AddHostedService<TcpServerService>();
builder.Services.AddHostedService<DefenseHostedService>();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseHttpsRedirection();
app.Run();

