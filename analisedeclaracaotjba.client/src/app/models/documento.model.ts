export interface Documento {
  _id: string; // Identificador único no MongoDB
  razaoSocial: string; // Nome/Razão Social
  cpfCnpj: string; // CPF ou CNPJ
  dataCertidao: Date; // Data da certidão
  validado: boolean; // Status de validação
  certidaoNumero: string; // Número da certidão
  endereco?: string; // Endereço, opcional
  dataPrazoCertidao: Date; // Prazo da certidão (dataCertidao + 30 dias)
  statusProcessamentoCertidao: string; // Status do processamento
  observacoes?: string; // Observações, opcional
  fileId: string; // Identificador do arquivo PDF
  processoList?: string[]; // Lista de processos, opcional
  emissor?: string; // Emissor da certidão
  logs: LogEntry[]; // Logs
  qrcode: string; // QR Code
  possuiArquivoPdf: boolean; // Indica se possui arquivo PDF
}

export interface LogEntry {
  acao: string; // Ação realizada
  data: Date; // Data da ação
  usuario: string; // Usuário responsável
  observacao?: string; // Observação, opcional
}
