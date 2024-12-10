using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Responses;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace AnaliseDeclaracaoTJBA.Server.Features.Services;
public class CertidaoProcessorService
{
    private readonly ILogger<CertidaoProcessorService> _logger;
    private readonly IMongoClient _mongoClient;
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processamentoTask;

    public CertidaoProcessorService(ILogger<CertidaoProcessorService> logger, IMongoClient mongoClient)
    {
        _logger = logger;
        _mongoClient = mongoClient;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://portalcertidoesws.tjba.jus.br/api/")
        };
    }

    public bool IsRunning => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

    public void StartProcessing()
    {
        if (IsRunning)
        {
            _logger.LogWarning("O processamento já está em execução.");
            return;
        }

        _logger.LogInformation("Iniciando o processamento...");
        _cancellationTokenSource = new CancellationTokenSource();
        _processamentoTask = Task.Run(() => ProcessarCertidoesAsync(_cancellationTokenSource.Token));
    }

    public async Task StopProcessingAsync()
    {
        if (!IsRunning)
        {
            _logger.LogWarning("O processamento já está parado.");
            return;
        }

        _logger.LogInformation("Parando o processamento...");
        _cancellationTokenSource?.Cancel();

        if (_processamentoTask != null)
        {
            await _processamentoTask;
            _processamentoTask = null;
        }

        _logger.LogInformation("Processamento parado com sucesso.");
    }

    private async Task ProcessarCertidoesAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CertidaoProcessorService iniciado.");

        var database = _mongoClient.GetDatabase("AnaliseTJBA");
        var collection = database.GetCollection<BsonDocument>("Documentos");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("validado", false),
                    Builders<BsonDocument>.Filter.Eq("statusProcessamentoCertidao", "Não Processado")
                );

                var documentos = await collection.Find(filter)
                    .Limit(50)
                    .ToListAsync(stoppingToken);

                if (!documentos.Any())
                {
                    _logger.LogInformation("Nenhum documento pendente encontrado.");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                foreach (var documento in documentos)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    var cpfCnpj = Regex.Replace(documento["cpfCnpj"].AsString, @"\D", "");
                    var certidaoNumero = documento["certidaoNumero"].AsString;

                    await AtualizarStatus(collection, documento, "Em processamento", "Documento em análise automática");

                    try
                    {
                        var response = await _httpClient.GetAsync($"pessoaJuridicaPrimeiroGrau/{cpfCnpj}/{certidaoNumero}", stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            var apiResponse = await response.Content.ReadFromJsonAsync<CertidaoResponse>();

                            if (apiResponse != null)
                            {
                                var situacao = apiResponse.Situacao ?? "Indefinida";
                                var validado = situacao == "0";

                                if (validado || situacao != "Indefinida")
                                {
                                    await AtualizarDocumento(collection, documento, situacao, validado, apiResponse.Endereco);
                                }
                                else
                                {
                                    await AtualizarErro(collection, documento, "Dados inconsistentes na API.");
                                }
                            }
                            else
                            {
                                await AtualizarErro(collection, documento, "Resposta da API nula ou inválida.");
                            }
                        }
                        else
                        {
                            await AtualizarErro(collection, documento, $"Erro na API TJBA: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await ReverterOuMarcarErro(collection, documento, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro geral no processamento: {ex.Message}");
            }
        }

        _logger.LogInformation("CertidaoProcessorService finalizado.");
    }

    private async Task AtualizarStatus(IMongoCollection<BsonDocument> collection, BsonDocument documento, string status, string observacao)
    {
        var update = Builders<BsonDocument>.Update
            .Set("statusProcessamentoCertidao", status)
            .Push("logs", new BsonDocument
            {
                { "acao", status },
                { "data", DateTime.UtcNow },
                { "usuario", "Sistema" },
                { "observacao", observacao }
            });

        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", documento["_id"]),
            update
        );
    }

    private async Task AtualizarDocumento(IMongoCollection<BsonDocument> collection, BsonDocument documento, string situacao, bool validado, string? endereco)
    {
        var resultadoValidacao = validado ? "Certidão válida" : "Certidão inválida";

        var update = Builders<BsonDocument>.Update
            .Set("statusCertidao", situacao)
            .Set("validado", validado)
            .Set("resultadoValidacao", resultadoValidacao)
            .Set("endereco", endereco != null ? (BsonValue)endereco : BsonNull.Value)
            .Set("statusProcessamentoCertidao", "Processado com sucesso")
            .Push("logs", new BsonDocument
            {
                { "acao", "Consulta automática na API do TJBA" },
                { "data", DateTime.UtcNow },
                { "usuario", "Sistema" },
                { "observacao", validado ? "Certidão confirmada válida." : "Certidão marcada como inválida." }
            });

        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", documento["_id"]),
            update
        );
    }

    private async Task AtualizarErro(IMongoCollection<BsonDocument> collection, BsonDocument documento, string erro)
    {
        var update = Builders<BsonDocument>.Update
            .Set("statusCertidao", "Erro")
            .Set("resultadoValidacao", erro)
            .Set("statusProcessamentoCertidao", "Erro no processamento")
            .Push("logs", new BsonDocument
            {
                { "acao", "Erro no processamento" },
                { "data", DateTime.UtcNow },
                { "usuario", "Sistema" },
                { "detalhes", erro }
            });

        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", documento["_id"]),
            update
        );

        _logger.LogError($"Erro ao processar documento {documento["_id"]}: {erro}");
    }

    private async Task ReverterOuMarcarErro(IMongoCollection<BsonDocument> collection, BsonDocument documento, string erro)
    {
        var tentativas = documento.Contains("tentativas") ? documento["tentativas"].AsInt32 : 0;
        tentativas++;

        if (tentativas >= 3)
        {
            await AtualizarErro(collection, documento, erro);
        }
        else
        {
            await AtualizarStatus(collection, documento, "Não Processado", $"Erro temporário. Tentativa {tentativas} de 3.");
        }
    }
}
