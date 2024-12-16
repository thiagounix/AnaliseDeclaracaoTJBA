using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.qrcode;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Text.RegularExpressions;

namespace AnaliseDeclaracaoTJBA.Server.Features.Shared;

public class CertidaoService
{
    private readonly IMongoDatabase _database;
    private readonly GridFSBucket _gridFS;
    private readonly HttpClient _httpClient;

    public CertidaoService(IMongoClient client)
    {
        _database = client.GetDatabase("AnaliseTJBA");
        _gridFS = new GridFSBucket(_database);
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7022/") // Configurável
        };
    }
    public async Task SalvarDocumentoComPdfAsync(
     IMongoCollection<BsonDocument> collection, byte[] pdfBytes, CertidaoDados certidaoDados)
    {
        var fileId = await SalvarArquivoPdfAsync($"{certidaoDados.CpfCnpj}.pdf", pdfBytes);

        var documento = new BsonDocument
    {
        { "cpfCnpj", certidaoDados.CpfCnpj },
        { "certidaoNumero", certidaoDados.CertidaoNumero },
        { "razaoSocial", certidaoDados.RazaoSocial != null ? (BsonValue)certidaoDados.RazaoSocial : BsonNull.Value },
        { "endereco", certidaoDados.Endereco != null ? (BsonValue)certidaoDados.Endereco : BsonNull.Value },
        { "dataCertidao", certidaoDados.DataCertidao != null ? (BsonValue)certidaoDados.DataCertidao : BsonNull.Value },
        { "dataPrazoCertidao", certidaoDados.DataCertidao != null ? (BsonValue)certidaoDados.DataCertidao.Value.AddDays(30) : BsonNull.Value },
        { "validado", false },
        { "modeloCertidao", BsonNull.Value},
        { "dataValidacao", BsonNull.Value },
        { "situacao", BsonNull.Value },
        { "tipoParticipacao", BsonNull.Value },
        { "statusProcessamentoCertidao", "Não Processado" },
        { "resultadoValidacao",BsonNull.Value },
        { "observacoes", BsonNull.Value },
        { "fileId", fileId },
        { "numSerie", BsonNull.Value },
        { "numDaje", BsonNull.Value },
        { "processoList", new BsonArray() },
        { "emissor", BsonNull.Value },
        { "qrcode", BsonNull.Value },
        { "logs", new BsonArray
              {
                  new BsonDocument
                  {
                      { "acao", "Documento processado via API externa" },
                      { "data", DateTime.UtcNow },
                      { "usuario", "Sistema" }
                  }
              }
          }
        };
        await collection.InsertOneAsync(documento);
    }


    public (bool Sucesso, string Mensagem) ValidarDadosCertidao(
     CertidaoDados certidaoDados,
     FornecedorRequest fornecedor)
    {
        // Verificar igualdade dos dados informados com os dados do PDF
        if (certidaoDados.CpfCnpj != fornecedor.CpfCnpj || certidaoDados.CertidaoNumero != fornecedor.CertidaoNumero)
        {
            return (false, "Dados informados não são os mesmos da certidão enviada.");
        }

        // Verificar situação "CONSTA/NÃO CONSTA"
        if (certidaoDados.Situacao == "CONSTAR")
        {
            return (false, "Certidão enviada CONSTA pendências.");
        }
        if (certidaoDados.Situacao != "NÃO CONSTAR")
        {
            return (false, "Situação da certidão é inválida.");
        }

        // Verificar validade da certidão (30 dias)
        if (certidaoDados.DataCertidao != null && certidaoDados.DataCertidao.Value.AddDays(30) < DateTime.UtcNow.Date)
        {
            return (false, $"Certidão vencida em {certidaoDados.DataCertidao.Value.AddDays(30):dd/MM/yyyy}, gere outra certidão válida.");
        }

        return (true, "Certidão válida e pronta para ser salva.");
    }

    public async Task<ObjectId> SalvarArquivoPdfAsync(string fileName, byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        return await _gridFS.UploadFromStreamAsync(fileName, stream);
    }

    // 2. Processar PDF: Extrair dados do PDF
    public CertidaoDados ProcessarPdf(byte[] pdfBytes)
    {
        var textoExtraido = ExtractPdfContent(pdfBytes);

        return new CertidaoDados
        {
            CertidaoNumero = ExtractRegexValue(textoExtraido, @"CERTIDÃO Nº:\s*(\w+)"),
            RazaoSocial = ExtractRegexValue(textoExtraido, @"Razão Social:\s*(.+)"),
            CpfCnpj = ExtractRegexValue(textoExtraido, @"CNPJ:\s*([\d./-]+)"),
            Endereco = ExtractRegexValue(textoExtraido, @"Endereço:\s*(.+)"),
            DataCertidao = DateTime.TryParse(
                ExtractRegexValue(textoExtraido, @"anteriores à data de (\d{2}/\d{2}/\d{4})"),
                out var data) ? data : null,
            Situacao = ExtractRegexValue(textoExtraido, @"(NÃO CONSTAR|CONSTAR)")
        };
    }


    // 4. Validar Certidão
    public async Task<(bool Validado, string Mensagem)> ValidarCertidaoAsync(string cpfCnpj, string certidaoNumero)
    {
        var payload = new { fornecedores = new[] { new { cpfCnpj, certidaoNumero } } };

        var response = await _httpClient.PostAsJsonAsync("api/validar-certidao", payload);
        if (response.IsSuccessStatusCode)
        {
            _ = await response.Content.ReadAsStringAsync();
            return (true, "Certidão validada com sucesso.");
        }
        return (false, "Falha ao validar certidão.");
    }

    // Auxiliares
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

// Classe auxiliar para transportar os dados extraídos
public class CertidaoDados
{
    public string? RazaoSocial { get; set; }
    public string? CpfCnpj { get; set; }
    public string? CertidaoNumero { get; set; }
    public string? Endereco { get; set; }
    public DateTime? DataCertidao { get; set; }
    public string? Situacao { get; set; }
}
