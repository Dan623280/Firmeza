var builder = WebApplication.CreateBuilder(args);

// Agrega soporte para Controllers
builder.Services.AddControllers();

// Agrega Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Redireccion HTTPS
app.UseHttpsRedirection();

// Activa las rutas de los Controllers
app.MapControllers();

app.Run();