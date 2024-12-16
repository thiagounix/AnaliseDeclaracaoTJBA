using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;
using AnaliseDeclaracaoTJBA.Server.Features.Shared;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions;

public static class ExternosEndpoints
{
    public static void MapEndpointsExternos(this WebApplication app)
    {
        _ = app.MapPost("/api/externo/enviar-certidao", async (IMongoClient client, EnviarCertidaoRequest request) =>
        {
            var service = new CertidaoService(client);

            try
            {
                // 1. Validar o payload
                if (request.Fornecedores == null || !request.Fornecedores.Any())
                {
                    return Results.BadRequest("Requisição inválida ou vazia.");
                }

                var resultados = new List<object>();
                var collection = client.GetDatabase("AnaliseTJBA").GetCollection<BsonDocument>("Documentos");

                foreach (var fornecedor in request.Fornecedores)
                {
                    try
                    {
                        // a) Converter PDF de Base64 para bytes
                        var pdfBytes = Convert.FromBase64String(fornecedor.FileCertidao);

                        // b) Processar o PDF e validar regras
                        var pdfData = service.ProcessarPdf(pdfBytes);

                        var (sucesso, mensagem) = service.ValidarDadosCertidao(pdfData, fornecedor);
                        if (!sucesso)
                        {
                            resultados.Add(new { fornecedor.CpfCnpj, fornecedor.CertidaoNumero, Validado = false, Mensagem = mensagem });
                            continue;
                        }

                        // c) Salvar documento e PDF no MongoDB
                        await service.SalvarDocumentoComPdfAsync(collection, pdfBytes, pdfData);

                        // d) Validar Certidão com endpoint externo
                        var validacao = await service.ValidarCertidaoAsync(pdfData.CpfCnpj, pdfData.CertidaoNumero);

                        resultados.Add(new
                        {
                            fornecedor.CpfCnpj,
                            fornecedor.CertidaoNumero,
                            validacao.Validado,
                            validacao.Mensagem
                        });
                    }
                    catch (Exception exFornecedor)
                    {
                        resultados.Add(new
                        {
                            fornecedor.CpfCnpj,
                            fornecedor.CertidaoNumero,
                            Validado = false,
                            Mensagem = $"Erro ao processar fornecedor: {exFornecedor.Message}"
                        });
                    }
                }

                return Results.Ok(new { resultados });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar requisição externa: {ex.Message}");
                return Results.Problem("Erro ao processar requisição externa.");
            }
        })
        .WithName("EnviarCertidaoExterna")
        .WithTags("Certidao Externa")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}