using AnaliseDeclaracaoTJBA.Server;
using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions;
using AnaliseDeclaracaoTJBA.Server.Features.Services;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Recuperar o ambiente atual
var environment = builder.Environment.EnvironmentName;
Console.WriteLine($"Ambiente atual: {environment}");

// Configurar Serviços
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        _ = policy.AllowAnyOrigin() // Permite qualquer origem
              .AllowAnyMethod() // Permite qualquer método HTTP
              .AllowAnyHeader(); // Permite qualquer cabeçalho
    });
});


builder.Services.AddSingleton<CertidaoProcessorService>();
//builder.Services.AddHostedService<CertidaoProcessorService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API de Documentos", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();
    // c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "YourProjectName.xml")); // Comente se não tiver XML
});


var mongoConnection = builder.Configuration.GetConnectionString("MongoDB");
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Exibe na console
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day) // Arquivo diário
    .WriteTo.Seq("http://localhost:5341") // Seq para visualização
    .CreateLogger();
Environment.SetEnvironmentVariable("ACCEPT_EULA", "Y");

builder.Host.UseSerilog();
var app = builder.Build();
app.UseCors("AllowAngularClient");


// Configurar Middleware de Logging para Depuração
app.Use(async (context, next) =>
{
    Console.WriteLine($"Recebendo requisição para {context.Request.Path}");
    await next.Invoke();
});
app.MapPost("/api/start-processing", (CertidaoProcessorService processor) =>
{
    processor.StartProcessing();
    return Results.Ok("Processamento iniciado.");
});

app.MapPost("/api/stop-processing", async (CertidaoProcessorService processor) =>
{
    await processor.StopProcessingAsync();
    return Results.Ok("Processamento pausado.");
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

// Endpoints organizados
app.MapEndpointsDocumentos();
app.MapEndpointsProcessarPDF();
app.MapConsumirEndPoint();

app.Run();