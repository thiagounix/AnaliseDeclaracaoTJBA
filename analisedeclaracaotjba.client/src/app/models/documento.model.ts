export interface Documento {
  _id?: string; // Identificador único
  nome: string; // Nome do documento
  tipo: string; // Tipo do arquivo
  dataCriacao: Date; // Data de envio
  validado: boolean; // Documento foi validado
  resultadoValidacao?: string; // Resultado da validação
  fileId?: string; // ID do arquivo no GridFS
}
