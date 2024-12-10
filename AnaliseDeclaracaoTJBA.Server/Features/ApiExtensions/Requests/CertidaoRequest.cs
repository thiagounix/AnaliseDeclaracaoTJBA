namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;

public class CertidaoRequest
{
    public List<Fornecedor> Fornecedores { get; set; }
}

public class Fornecedor
{
    public string razaoSocial { get; set; }
    public string cpfCnpj { get; set; }
    public string Documento { get; set; }
    public string certidaoNumero { get; set; }
    public string Validade { get; set; }
}
public enum modeloCertidao
{
    CONCORDATA_E_FALENCIA = 3
}
