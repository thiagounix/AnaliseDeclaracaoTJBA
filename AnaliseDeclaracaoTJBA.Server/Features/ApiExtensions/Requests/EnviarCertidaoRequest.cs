namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions.Requests;

public class EnviarCertidaoRequest
{
    public required List<FornecedorRequest> Fornecedores { get; set; }
}

public class FornecedorRequest
{
    public required string CpfCnpj { get; set; }
    public required string CertidaoNumero { get; set; }
    public required string FileCertidao { get; set; }
}
