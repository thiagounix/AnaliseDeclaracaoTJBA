using AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AnaliseDeclaracaoTJBA.Server.Features.Services;

public class DataExtractorService
{
    public List<CertidaoRequest> ExtractFromString(string rawData)
    {
        // Assumindo que os dados estão em formato JSON, deserializamos para uma lista de CertidaoRequest
        try
        {
            var certidaoList = JsonSerializer.Deserialize<List<CertidaoRequest>>(rawData);
            return certidaoList ?? new List<CertidaoRequest>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao processar os dados da string.", ex);
        }
    }

    public List<CertidaoRequest> ExtractFromDatabase(string databaseResponse)
    {
        // Exemplo de parsing: normalização de dados recebidos de outra base de dados
        var certidaoList = new List<CertidaoRequest>();

        try
        {
            var registros = JsonSerializer.Deserialize<List<dynamic>>(databaseResponse);
            if (registros == null) return certidaoList;

            foreach (var registro in registros)
            {
                certidaoList.Add(new CertidaoRequest
                {
                    Fornecedores = new List<Fornecedor>
                    {
                        new Fornecedor
                        {
                            razaoSocial = registro.razaoSocial.ToString(),
                            cpfCnpj = registro.cpfCnpj.ToString(),
                            certidaoNumero = registro.certidaoNumero.ToString(),
                            Validade = registro.validade.ToString(),
                            Documento = registro.Documento?.ToString() ?? "CONCORDATA E FALENCIA"
                        }
                    }
                });
            }

            return certidaoList;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao processar os dados da base de dados.", ex);
        }
    }

    public List<CertidaoRequest> ExtractFromPdf(byte[] pdfBytes)
    {
        var certidaoList = new List<CertidaoRequest>();

        try
        {
            using (var reader = new PdfReader(pdfBytes))
            {
                for (int page = 1; page <= reader.NumberOfPages; page++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(reader, page);

                    // Extração usando Regex
                    var matches = Regex.Matches(text, @"Nome:\s*(?<Nome>.+)\nCPF\/CNPJ:\s*(?<CpfCnpj>.+)\nNúmero:\s*(?<Numero>.+)\nValidade:\s*(?<Validade>.+)");
                    foreach (Match match in matches)
                    {
                        certidaoList.Add(new CertidaoRequest
                        {
                            Fornecedores = new List<Fornecedor>
                            {
                                new Fornecedor
                                {
                                    razaoSocial = match.Groups["Nome"].Value.Trim(),
                                    cpfCnpj = match.Groups["CpfCnpj"].Value.Trim(),
                                    certidaoNumero = match.Groups["Numero"].Value.Trim(),
                                    Validade = match.Groups["Validade"].Value.Trim(),
                                    Documento = "CONCORDATA E FALENCIA" // Valor padrão para Documento
                                }
                            }
                        });
                    }
                }
            }

            return certidaoList;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao processar os dados do PDF.", ex);
        }
    }
}