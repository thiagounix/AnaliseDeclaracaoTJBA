import { Component, OnInit } from '@angular/core';
import { DocumentosService } from '../../services/documentos.service';
import { Documento } from '../../models/documento.model';

@Component({
  selector: 'app-documentos-list',
  templateUrl: './documentos-list.component.html',
  styleUrls: ['./documentos-list.component.css']
})
export class DocumentosListComponent implements OnInit {
  [x: string]: any;
  documentos: any[] = [];
  filtro: string = '';

  constructor(private documentoService: DocumentosService) { }

  ngOnInit(): void {
    this.carregarDocumentos();
  }

  carregarDocumentos(): void {
    this.documentoService.getDocumentos().subscribe({
      next: (data: Documento[]) => {
        console.log('Documentos carregados:', data); // Verifique se _id está presente nos dados
        this.documentos = data;
      },
      error: (err) => {
        console.error('Erro ao carregar documentos:', err);
      }
    });
  }

  filtrarDocumentos(): void {
    const filtroLower = this.filtro.toLowerCase();
    this.documentos = this.documentos.filter(documento =>
      documento.cpfCnpj.toLowerCase().includes(filtroLower) ||
      documento.certidaoNumero.toLowerCase().includes(filtroLower) ||
      documento.statusProcessamentoCertidao.toLowerCase().includes(filtroLower)
    );
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
      error: (err) => {
        console.error('Erro ao baixar documento:', err);
      }
    });
  }
}
