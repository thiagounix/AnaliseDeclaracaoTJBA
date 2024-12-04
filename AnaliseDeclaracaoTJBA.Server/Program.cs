using AnaliseDeclaracaoTJBA.Server;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Recuperar o ambiente atual
var environment = builder.Environment.EnvironmentName;
Console.WriteLine($"Ambiente atual: {environment}");

// Configurar Serviços
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy.WithOrigins("http://localhost:62088", "https://localhost:62088") // URLs do Angular
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API de Documentos", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();
});


var mongoConnection = builder.Configuration.GetConnectionString("MongoDB");
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));

var app = builder.Build();
app.UseCors("AllowAngularClient");
// Configurar Middleware de Logging para Depuração
app.Use(async (context, next) =>
{
    Console.WriteLine($"Recebendo requisição para {context.Request.Path}");
    await next.Invoke();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

app.MapGet("/api/documentos", async (IMongoClient client, int page = 1, int pageSize = 10) =>
{
    try
    {
        if (page < 1 || pageSize < 1)
        {
            return Results.BadRequest("Os parâmetros de página e tamanho da página devem ser maiores que 0.");
        }

        var database = client.GetDatabase("AnaliseTJBA");
        var collection = database.GetCollection<BsonDocument>("Documentos");

        // Calcula o total de documentos para paginação
        var totalDocuments = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
        var totalPages = (int)Math.Ceiling((double)totalDocuments / pageSize);

        // Recupera os documentos paginados
        var documentos = await collection.Find(Builders<BsonDocument>.Filter.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending("dataCriacao")) // Ordena pelos mais recentes
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        // Retorna os dados em um formato amigável
        var result = new
        {
            totalPages,
            currentPage = page,
            pageSize,
            totalDocuments,
            data = documentos.Select(d => new
            {
                _id = d["_id"].ToString(),
                nome = d["razaoSocial"].AsString,
                certidao = d["certidao"].AsString,
                dataCriacao = d["dataCriacao"].ToUniversalTime(),
                validado = d["validado"].AsBoolean,
                resultadoValidacao = d["resultadoValidacao"].IsBsonNull ? null : d["resultadoValidacao"].AsString
            })
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao carregar documentos: {ex.Message}");
        return Results.Problem("Erro ao carregar os documentos.");
    }
});

app.MapGet("/api/documentos/{id}/download", async (IMongoClient client, string id) =>
{
    try
    {
        var database = client.GetDatabase("AnaliseTJBA");
        var gridFS = new GridFSBucket(database);

        var objectId = ObjectId.Parse(id);
        using var memoryStream = new MemoryStream();
        await gridFS.DownloadToStreamAsync(objectId, memoryStream);
        memoryStream.Position = 0;

        return Results.File(memoryStream.ToArray(), "application/pdf", $"{id}.pdf");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao baixar o documento: {ex.Message}");
        return Results.Problem("Erro ao baixar o documento.");
    }
});



app.MapPost("/api/processar-pdf", async (IMongoClient client, HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var arquivo = form.Files.FirstOrDefault();

        if (arquivo == null || arquivo.ContentType != "application/pdf")
        {
            return Results.BadRequest("Envie um arquivo PDF válido.");
        }

        using var stream = new MemoryStream();
        await arquivo.CopyToAsync(stream);

        // Usando iTextSharp para ler o conteúdo do PDF
        var textoExtraido = "";
        using (var reader = new PdfReader(stream.ToArray()))
        {
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                textoExtraido += PdfTextExtractor.GetTextFromPage(reader, i);
            }
        }

        // Regex para extrair dados
        var certidaoRegex = new Regex(@"CERTIDÃO Nº:\s*(\w+)");
        var razaoRegex = new Regex(@"Razão Social:\s*(.+)");
        var cnpjRegex = new Regex(@"CNPJ:\s*([\d./-]+)");
        var enderecoRegex = new Regex(@"Endereço:\s*(.+)");
        var statusRegex = new Regex(@"(NÃO CONSTAR)");
        var dataRegex = new Regex(@"anteriores à data de (\d{2}/\d{2}/\d{4})");

        // Extração de dados
        var certidao = certidaoRegex.Match(textoExtraido).Groups[1].Value;
        var razaoSocial = razaoRegex.Match(textoExtraido).Groups[1].Value;
        var cnpj = cnpjRegex.Match(textoExtraido).Groups[1].Value;
        var endereco = enderecoRegex.Match(textoExtraido).Groups[1].Value;
        var status = statusRegex.Match(textoExtraido).Value;
        var dataExtraida = dataRegex.Match(textoExtraido).Groups[1].Value;

        // Validações
        var validado = false;
        var resultadoValidacao = "Pendente";

        var dataExtraidaDate = DateTime.Parse(dataExtraida);
        var hojeMais30 = DateTime.UtcNow.AddDays(30);

        if (dataExtraidaDate > hojeMais30)
        {
            validado = true;
            resultadoValidacao = "Vencido";
        }
        else if (status == "CONSTAR")
        {
            validado = true;
            resultadoValidacao = "Consta";
        }

        // Armazenar no MongoDB
        var database = client.GetDatabase("AnaliseTJBA");
        var collection = database.GetCollection<BsonDocument>("Documentos");

        var documento = new BsonDocument
        {
            { "certidao", certidao },
            { "razaoSocial", razaoSocial },
            { "cnpj", cnpj },
            { "endereco", endereco },
            { "status", status },
            { "dataExtraida", dataExtraida },
            { "validado", validado },
            { "resultadoValidacao", resultadoValidacao },
            { "dataCriacao", DateTime.UtcNow },
            { "arquivo", stream.ToArray() } // Salvar o arquivo no GridFS, opcional
        };

        await collection.InsertOneAsync(documento);

        return Results.Ok(new { mensagem = "Documento processado com sucesso.", validado, resultadoValidacao });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao processar PDF: {ex.Message}");
        return Results.Problem("Erro ao processar o documento.");
    }
}).WithMetadata(new EndpointNameMetadata("UploadDocumento")); // Define OperationId

app.Run();