using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Text.RegularExpressions;

namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions;

public static class EndpointsProcessarPdf
{
    public static void MapEndpointsProcessarPDF(this WebApplication app)
    {
        _ = app.MapPost("/api/processar-pdf", async (IMongoClient client, HttpRequest request) =>
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

                // Extrai o texto do PDF
                var textoExtraido = ExtractPdfContent(stream.ToArray());

                // Aplica regex para extrair informações
                var certidaoNumero = ExtractRegexValue(textoExtraido, @"CERTIDÃO Nº:\s*(\w+)");
                var razaoSocial = ExtractRegexValue(textoExtraido, @"Razão Social:\s*(.+)");
                var cpfCnpj = ExtractRegexValue(textoExtraido, @"CNPJ:\s*([\d./-]+)");
                var endereco = ExtractRegexValue(textoExtraido, @"Endereço:\s*(.+)");
                var dataCertidao = ExtractRegexValue(textoExtraido, @"anteriores à data de (\d{2}/\d{2}/\d{4})");
                var constaNaoConsta = ExtractRegexValue(textoExtraido, @"(NÃO CONSTAR|CONSTAR)");

                // Calcula validade com base na data extraída
                DateTime? dataPrazoCertidao = null;
                dataPrazoCertidao = DateTime.Parse(dataCertidao).AddDays(30);
               

                // Salva o arquivo no GridFS
                var database = client.GetDatabase("AnaliseTJBA");
                var gridFS = new GridFSBucket(database);
                var fileId = await gridFS.UploadFromStreamAsync(arquivo.FileName, stream);

                if (fileId == null)
                {
                    throw new Exception("Erro ao salvar arquivo no GridFS.");
                }
                // Cria o documento a ser salvo no MongoDB
                var collection = database.GetCollection<BsonDocument>("Documentos");
                var documento = new BsonDocument
{
    { "razaoSocial", razaoSocial },
    { "cpfCnpj", cpfCnpj },
    { "dataCertidao",  DateTime.Parse(dataCertidao)},
    { "dataPrazoCertidao", dataPrazoCertidao },
    { "validado", false },
    {"dataValidacao", BsonNull.Value },
    { "certidaoNumero", certidaoNumero },
    { "situacaoDocumentoEnviado", constaNaoConsta == "NÃO CONSTAR" ? "0" : "1" },
    { "endereco", endereco },
    { "statusProcessamentoCertidao", "Não Processado" },
    {"resultadoValidacao",BsonNull.Value },
    { "observacoes", BsonNull.Value },
    { "fileId", fileId },
    { "processoList", new BsonArray() }, // Inicializa vazio
    { "emissor", BsonNull.Value },
    { "logs", new BsonArray
        {
            new BsonDocument
            {
                { "acao", "Documento enviado pelo frontend" },
                { "data", DateTime.UtcNow },
                { "usuario", "Sistema" }
            }
        }
    },
    { "qrcode", BsonNull.Value } // Inicializa como null
};


                await collection.InsertOneAsync(documento);

                return Results.Ok(new
                {
                    mensagem = "Documento Extraido e Salvo com sucesso.",
                    certidaoNumero,
                    cpfCnpj
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar PDF: {ex.Message}");
                return Results.Problem("Erro ao processar o documento.");
            }
        });
    }

    private static string ExtractPdfContent(byte[] pdfContent)
    {
        using var reader = new PdfReader(pdfContent);
        var textoExtraido = string.Empty;

        for (int i = 1; i <= reader.NumberOfPages; i++)
        {
            textoExtraido += PdfTextExtractor.GetTextFromPage(reader, i);
        }

        return textoExtraido;
    }

    private static string? ExtractRegexValue(string texto, string pattern)
    {
        var match = Regex.Match(texto, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}