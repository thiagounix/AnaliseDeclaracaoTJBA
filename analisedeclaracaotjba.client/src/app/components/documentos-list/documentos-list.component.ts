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
  documentos: Documento[] = [];
  filtroGeral: string = '';
  cpfCnpj: string = '';
  certidaoNumero: string = '';
  status: string = '';
  isLoading: boolean = false;

  constructor(private documentoService: DocumentosService) { }

  ngOnInit(): void {
    this.carregarDocumentos();
  }

  carregarDocumentos(): void {
    this.isLoading = true;
    this.documentoService.getDocumentos().subscribe({
      next: (data: Documento[]) => {
        this.documentos = data.sort((a, b) => a.razaoSocial.localeCompare(b.razaoSocial));
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar documentos:', err);
        this.isLoading = false;
      },
    });
  }

  filtrarDocumentos(): void {
    if (!this.filtroGeral.trim()) {
      this.carregarDocumentos();
      return;
    }

    this.isLoading = true;

    const filtroLower = this.filtroGeral.trim().toLowerCase();

    this.documentos = this.documentos.filter(documento => {
      const cpfCnpjMatch = documento.cpfCnpj?.toLowerCase().includes(filtroLower);
      const certidaoNumeroMatch = documento.certidaoNumero?.toLowerCase().includes(filtroLower);
      const statusMatch = documento.statusProcessamentoCertidao?.toLowerCase() === filtroLower;

      return cpfCnpjMatch || certidaoNumeroMatch || statusMatch;
    });
    this.documentos.sort((a, b) => a.razaoSocial.localeCompare(b.razaoSocial));
    this.isLoading = false;
  }

  limparFiltros(): void {
    this.filtroGeral = '';
    this.carregarDocumentos();
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
  getGoogleMapsUrl(endereco: string): string {
    if (!endereco) {
      return '';
    }
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(endereco)}`;
  }
  isCertidaoVencida(dataPrazoCertidao: string | Date): boolean {
    const validade = new Date(dataPrazoCertidao);
    const hoje = new Date();
    return validade < hoje;
  }

  isEnviadoPorLote(logs: LogEntry[]): boolean {
    const acao = logs[0]?.acao;
    return acao === 'Inserido via consulta API'; // Botão desabilitado para documentos enviados por lote
  }
}
