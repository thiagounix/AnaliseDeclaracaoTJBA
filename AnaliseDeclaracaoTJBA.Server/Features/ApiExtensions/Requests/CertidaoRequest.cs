namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;

public class CertidaoRequest
{
    public required List<Fornecedor> Fornecedores { get; set; }
}

public class Fornecedor
{
    public required string razaoSocial { get; set; }
    public required string cpfCnpj { get; set; }
    public required string Documento { get; set; }
    public required string certidaoNumero { get; set; }
    public required string Validade { get; set; }
}
public enum modeloCertidao
{
    CONCORDATA_E_FALENCIA = 3
}
