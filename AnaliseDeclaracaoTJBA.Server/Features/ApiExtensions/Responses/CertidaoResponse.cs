using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Responses;

public class CertidaoResponse
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; } // Identificador único no MongoDB (Obrigatório*)

    [BsonElement("razaoSocial")]
    public required string RazaoSocial { get; set; } // Nome da empresa ou pessoa física (Obrigatório*)

    [BsonElement("cpfCnpj")]
    public required string CpfCnpj { get; set; } // CPF ou CNPJ no formato esperado (Obrigatório*)

    [BsonElement("dataCertidao")]
    public DateTime DataCertidao { get; set; } // Data da certidão retirada do PDF (Obrigatório*)

    [BsonElement("validado")]
    public bool Validado { get; set; } // Indica se foi validado ou não (Obrigatório*)

    [BsonElement("certidaoNumero")]
    public required string CertidaoNumero { get; set; } // Número da certidão (Obrigatório*)

    [BsonElement("endereco")]
    public string? Endereco { get; set; } // Endereço (Não Obrigatório)

    [BsonElement("dataPrazoCertidao")]
    public DateTime DataPrazoCertidao { get; set; } // DataCertidao + 30 dias (Obrigatório*)

    [BsonElement("statusProcessamentoCertidao")]
    public required string StatusProcessamentoCertidao { get; set; } // Status do processamento da certidão (Obrigatório*)

    [BsonElement("observacoes")]
    public string? Observacoes { get; set; } // Campo para intercorrências (Não Obrigatório)

    [BsonElement("fileId")]
    public required string FileId { get; set; } // Identificador do arquivo no GridFS (Obrioque pode ser?
                                                // gatório*)

    [BsonElement("processoList")]
    public List<string>? ProcessoList { get; set; } // Lista de processos, se houver (Não Obrigatório)

    [BsonElement("emissor")]
    public string? Emissor { get; set; } // Emissor da certidão (Não Obrigatório)

    [BsonElement("logs")]
    public List<LogEntry> Logs { get; set; } = new(); // Logs do processamento (Obrigatório*)

    [BsonElement("situacao")]
    public required string Situacao { get; set; } // Situação da certidão ("0" para válida, outros valores para inválida)

    [BsonElement("qrcode")]
    public required string Qrcode { get; set; } // QR Code em formato base64 (Obrigatório*)
}

public class LogEntry
{
    [BsonElement("acao")]
    public required string Acao { get; set; } // Ação realizada

    [BsonElement("data")]
    public DateTime Data { get; set; } // Data da ação

    [BsonElement("usuario")]
    public required string Usuario { get; set; } // Usuário responsável pela ação

    [BsonElement("observacao")]
    public string? Observacao { get; set; } // O
}
