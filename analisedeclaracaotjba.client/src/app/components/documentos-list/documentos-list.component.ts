import { Component, OnInit } from '@angular/core';
import { DocumentosService } from '../../services/documentos.service';
import { Documento, LogEntry } from '../../models/documento.model';

@Component({
    selector: 'app-documentos-list',
    templateUrl: './documentos-list.component.html',
    styleUrls: ['./documentos-list.component.css'],
    standalone: false
})
export class DocumentosListComponent implements OnInit {
  [x: string]: any;
  documentos: any[] = [];
  filtro: string = '';
  isLoading: boolean = false; // Para controlar o estado de carregamento
  constructor(private documentoService: DocumentosService) { }

  ngOnInit(): void {
    this.carregarDocumentos();
  }

  carregarDocumentos(): void {
    this.isLoading = true;
    this.documentoService.getDocumentos().subscribe({
      next: (data: Documento[]) => {
        this.documentos = data;
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Erro ao carregar documentos:', err);
        this.isLoading = false;
      },
    });
  }
  filtrarDocumentos(): void {
    this.isLoading = true;

    const filtroLower = this.filtro.trim().toLowerCase();

    this.documentoService.getDocumentos(filtroLower).subscribe({
      next: (data: Documento[]) => {
        this.documentos = data;
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Erro ao filtrar documentos:', err);
        this.isLoading = false;
      }
    });
  }
  limparFiltro(): void {
    this.filtro = ''; // Redefine o valor do filtro para vazio
    this.carregarDocumentos(); // Recarrega a lista completa de documentos
  }

  baixarDocumento(documentId: string, razaoSocial: string): void {
    this.documentoService.downloadDocumento(documentId).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${razaoSocial}.pdf`;
        a.click();
      },
      error: (err: any) => {
        console.error('Erro ao baixar documento:', err);
      }
    });
  }
  getTipoEnvio(logs: LogEntry[]): string {
    const acao = logs[0]?.acao; // Assume o primeiro log como base
    switch (acao) {
      case 'Inserido via consulta API':
        return 'Enviado por Lote';
      case 'Documento enviado pelo frontend':
        return 'Enviado pelo Sistema';
      case 'Enviado via integração': // Futura implementação
        return 'Enviado via Integração';
      default:
        return 'Tipo Desconhecido';
    }
  }
  isCertidaoVencida(documento: Documento): boolean {
    const validade = new Date(documento.dataPrazoCertidao);
    const hoje = new Date();
    return validade < hoje;
  }
  isEnviadoPorLote(logs: LogEntry[]): boolean {
    const acao = logs[0]?.acao;
    return acao === 'Inserido via consulta API'; // Botão desabilitado para documentos enviados por lote
  }
}
