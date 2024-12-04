import { Component, OnInit } from '@angular/core';
import { DocumentosService } from '../services/documentos.service';
import { Documento } from '../models/documento.model';

@Component({
  selector: 'app-documentos',
  templateUrl: './documentos.component.html',
  styleUrls: ['./documentos.component.css']
})
export class DocumentosComponent implements OnInit {
  documentos: Documento[] = []; // Lista de documentos carregados
  nome: string = ''; // Nome do documento
  arquivo!: File; // Arquivo selecionado
    totalPages: any;
    currentPage: any;

  constructor(private documentosService: DocumentosService) { }

  ngOnInit(): void {
    this.carregarDocumentos(); // Carrega os documentos ao iniciar
  }

  // Método para carregar documentos do backend
  carregarDocumentos(page: number = 1): void {
    this.documentosService.getDocumentos(page).subscribe({
      next: (response) => {
        this.documentos = response.data;
        this.totalPages = response.totalPages;
        this.currentPage = response.currentPage;
      },
      error: (err) => {
        console.error('Erro ao carregar documentos:', err);
      }
    });
  }


  // Método para capturar o arquivo selecionado pelo usuário
  selecionarArquivo(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.arquivo = input.files[0]; // Define o arquivo selecionado
    }
  }

  // Método para enviar documento ao backend
  enviarDocumento(): void {
    if (!this.nome || !this.arquivo) {
      alert('Por favor, preencha o nome e selecione um arquivo PDF.');
      return;
    }

    // Criar o objeto FormData para envio multipart
    const formData = new FormData();
    formData.append('nome', this.nome);
    formData.append('arquivo', this.arquivo);

    this.documentosService.uploadDocumento(formData).subscribe({
      next: () => {
        alert('Documento enviado com sucesso!');
        this.carregarDocumentos(); // Recarrega a lista após envio
        this.nome = ''; // Reseta o nome
        this.arquivo = undefined!; // Reseta o arquivo
      },
      error: (err) => {
        console.error('Erro ao enviar documento:', err);
        alert('Erro ao enviar documento.');
      }
    });
  }
}
