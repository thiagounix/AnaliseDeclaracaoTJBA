import { Component } from '@angular/core';
import { DocumentosService } from '../../services/documentos.service';

@Component({
    selector: 'app-documentos-upload',
    templateUrl: './documentos-upload.component.html',
    styleUrls: ['./documentos-upload.component.css'],
    standalone: false
})
export class DocumentosUploadComponent {
  arquivo!: File; // Arquivo selecionado

  constructor(private documentosService: DocumentosService) { }

  selecionarArquivo(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.arquivo = input.files[0];
    }
  }

  enviarDocumento(): void {
    if (!this.arquivo) {
      alert('Por favor, selecione um arquivo PDF.');
      return;
    }

    const formData = new FormData();
    formData.append('arquivo', this.arquivo);

    this.documentosService.uploadDocumento(formData).subscribe({
      next: () => {
        alert('Documento enviado com sucesso!');
        this.arquivo = undefined!; // Reseta o arquivo selecionado
      },
      error: (err) => {
        console.error('Erro ao enviar documento:', err);
        alert('Erro ao enviar documento.');
      },
    });
  }
}
