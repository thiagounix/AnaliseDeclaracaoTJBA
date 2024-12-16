namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;

public class CertidaoRequest
{
    public required List<Fornecedor> Fornecedores { get; set; }
}

public class Fornecedor
{
    public required string cpfCnpj { get; set; }
    public required string certidaoNumero { get; set; }
}