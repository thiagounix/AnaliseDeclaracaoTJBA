using AnaliseDeclaracaoTJBA.Server;
using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions;
using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;
using Microsoft.OpenApi.Any;
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


//builder.Services.AddSingleton<CertidaoProcessorService>(); para start stop end point
//builder.Services.AddHostedService<CertidaoProcessorService>(); para executar background service
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API de Documentos", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();

    c.MapType<EnviarCertidaoRequest>(() => new OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["fornecedores"] = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["cpfCnpj"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("12.345.678/0001-99") },
                        ["certidaoNumero"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("12345678") },
                        ["fileCertidao"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("base64_pdf_content_here") }
                    }
                }
            }
        }
    });
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

app.Use(async (context, next) =>
{
    Console.WriteLine($"Recebendo requisição para {context.Request.Path}");
    await next.Invoke();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

// Endpoints organizados
app.MapEndpointsDocumentos();
app.MapEndpointsProcessarPDF();
app.MapConsumirEndPoint();
app.MapEndpointsExternos();
app.Run();