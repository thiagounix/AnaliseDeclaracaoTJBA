using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions;

public static class ConsumirEndPoint
{
    public static void MapConsumirEndPoint(this WebApplication app)
    {
        _ = app.MapPost("/api/validar-certidao", async (IMongoClient client, CertidaoRequest request) =>
        {
            var stopwatch = Stopwatch.StartNew();

            // Contadores
            int validadoCount = 0;
            int invalidoCount = 0;
            int erroCount = 0;
            int totalProcessados = 0;
            var erroIds = new List<string>();

            try
            {
                if (request.Fornecedores == null || !request.Fornecedores.Any())
                {
                    return Results.BadRequest("A lista de fornecedores está vazia.");
                }

                using var httpClient = new HttpClient { BaseAddress = new Uri("https://portalcertidoesws.tjba.jus.br/api/") };
                var database = client.GetDatabase("AnaliseTJBA");
                var collection = database.GetCollection<BsonDocument>("Documentos");

                var resultados = new List<object>();

                foreach (var fornecedor in request.Fornecedores)
                {
                    totalProcessados++;

                    try
                    {
                        var cpfCnpjNormalizado = Regex.Replace(fornecedor.cpfCnpj, @"\D", "");
                        var certidaoNumero = fornecedor.certidaoNumero;

                        // Chamada ao endpoint externo
                        var response = await httpClient.GetAsync($"pessoaJuridicaPrimeiroGrau/{cpfCnpjNormalizado}/{certidaoNumero}");
                        if (!response.IsSuccessStatusCode)
                        {
                            erroCount++;
                            erroIds.Add($"CPF/CNPJ: {fornecedor.cpfCnpj}, Certidão: {fornecedor.certidaoNumero}");
                            resultados.Add(new
                            {
                                fornecedor.cpfCnpj,
                                fornecedor.certidaoNumero,
                                mensagem = $"Erro ao consultar API: {response.StatusCode}"
                            });
                            continue;
                        }

                        var apiResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                        if (apiResponse.ValueKind == JsonValueKind.Object)
                        {
                            // Obtém a propriedade 'situacao'
                            var situacao = apiResponse.TryGetProperty("situacao", out var situacaoElement)
                                ? situacaoElement.GetString()
                                : null;

                            bool validado = situacao == "0";
                            if (validado)
                            {
                                validadoCount++;
                            }
                            else
                            {
                                invalidoCount++;
                            }

                            var resultadoValidacao = validado
                                ? "Certidão válida"
                                : string.IsNullOrEmpty(situacao)
                                    ? "Situação não informada"
                                    : "Certidão inválida";

                            var endereco = apiResponse.TryGetProperty("endereco", out var enderecoElement)
                                ? enderecoElement.GetString()
                                : null;
                            var razaoSocial = apiResponse.TryGetProperty("razaoSocial", out var razaoSocialElement)
                                 ? razaoSocialElement.GetString()
                                 : null;
                            var dataCertidao = apiResponse.TryGetProperty("dataCriacao", out var dataCriacaoElement)
                                 ? dataCriacaoElement.GetString()
                                 : null;
                            var qrCode = apiResponse.TryGetProperty("qrcode", out var qrcodeElement)
                              ? qrcodeElement.GetString()
                              : null;
                            DateTime? dataPrazoCertidao = null;
                            if (DateTime.TryParse(dataCertidao, out var dataParsed))
                            {
                                dataPrazoCertidao = dataParsed.AddDays(30);
                            }
                            // Busca documento no MongoDB
                            var filter = Builders<BsonDocument>.Filter.Eq("cpfCnpj", fornecedor.cpfCnpj) &
                                         Builders<BsonDocument>.Filter.Eq("certidaoNumero", fornecedor.certidaoNumero);
                            var documento = await collection.Find(filter).FirstOrDefaultAsync();
                            // Calcula validade com base na data extraída
                           
                            if (documento == null)
                            {
                                // Cria novo documento se não existir
                                var novoDocumento = new BsonDocument
                                {
                                    { "cpfCnpj", fornecedor.cpfCnpj },
                                    { "certidaoNumero", fornecedor.certidaoNumero },
                                    { "razaoSocial", razaoSocial?.Trim() },
                                    {"dataCertidao",dataCertidao  },
                                    { "dataPrazoCertidao", dataPrazoCertidao},
                                    { "statusProcessamentoCertidao", "Processado" },
                                    { "validado", validado },
                                    { "resultadoValidacao", resultadoValidacao },
                                    { "dataValidacao", DateTime.UtcNow },
                                    { "modeloCertidao" , 4 },
                                    { "endereco", endereco != null ? (BsonValue)endereco : BsonNull.Value },
                                    {"qrcode",qrCode },
                                    { "logs", new BsonArray
                                        {
                                            new BsonDocument
                                            {
                                                { "acao", "Inserido via consulta API" },
                                                { "data", DateTime.UtcNow },
                                                { "usuario", "Sistema" }
                                            }
                                        }
                                    }
                                };
                                await collection.InsertOneAsync(novoDocumento);
                            }
                            else
                            {
                                // Atualiza documento existente
                                var update = Builders<BsonDocument>.Update
                                    .Set("statusProcessamentoCertidao", "Processado")
                                    .Set("validado", validado)
                                    .Set("resultadoValidacao", resultadoValidacao)
                                    .Set("razaoSocial", razaoSocial?.Trim())
                                    .Set("DataPrazoCertidao", dataPrazoCertidao)
                                    .Set("dataValidacao", DateTime.UtcNow)
                                    .Set("dataCertidao", dataCertidao)
                                    .Set("modeloCertidao", 4)
                                    .Set("endereco", endereco != null ? (BsonValue)endereco : BsonNull.Value)
                                    .Push("logs", new BsonDocument
                                    {
                                        { "acao", "Consulta na API do TJBA" },
                                        { "data", DateTime.UtcNow },
                                        { "usuario", "Sistema" },
                                        { "observacao", validado ? "Certidão confirmada válida." : "Certidão não válida ou erro na validação." }
                                    });

                                _ = await collection.UpdateOneAsync(filter, update);
                            }

                            resultados.Add(new
                            {
                                razaoSocial,
                                fornecedor.cpfCnpj,
                                fornecedor.certidaoNumero,
                                validado,
                                resultadoValidacao
                            });
                        }
                        else
                        {
                            erroCount++;
                            erroIds.Add($"CPF/CNPJ: {fornecedor.cpfCnpj}, Certidão: {fornecedor.certidaoNumero}");
                            resultados.Add(new
                            {
                                fornecedor.cpfCnpj,
                                fornecedor.certidaoNumero,
                                mensagem = "Resposta inválida ou formato inesperado da API externa."
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        erroCount++;
                        erroIds.Add($"CPF/CNPJ: {fornecedor.cpfCnpj}, Certidão: {fornecedor.certidaoNumero}");
                        resultados.Add(new
                        {
                            fornecedor.cpfCnpj,
                            fornecedor.certidaoNumero,
                            mensagem = $"Erro ao processar: {ex.Message}"
                        });
                    }
                }

                stopwatch.Stop();

                // Log final
                return Results.Ok(new
                {
                    mensagem = "Processamento concluído.",
                    log = new
                    {
                        totalProcessados,
                        validadoCount,
                        invalidoCount,
                        erroCount,
                        erroIds,
                        tempoDecorridoSegundos = stopwatch.Elapsed.TotalSeconds
                    },
                    resultados
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"Erro geral: {ex.Message}");
                return Results.Problem("Erro ao processar a requisição.");
            }
        });
    }
}