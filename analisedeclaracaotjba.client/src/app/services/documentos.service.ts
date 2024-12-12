import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { catchError, map, Observable, throwError } from 'rxjs';
import { Documento } from '../models/documento.model';

@Injectable({
  providedIn: 'root',
})
export class DocumentosService {
  private apiUrlListar = '/api/documentos-list'; // URL base para a API
  private apiUrlProcessar = '/api/processar-pdf'; // URL para upload

  constructor(private http: HttpClient) { }

  getDocumentos(
    cpfCnpj?: string,
    certidaoNumero?: string,
    status?: string,

  ): Observable<Documento[]> {
    let params = new HttpParams();

    if (cpfCnpj) {
      params = params.set('cpfCnpj', cpfCnpj.trim());
    }
    if (certidaoNumero) {
      params = params.set('certidaoNumero', certidaoNumero.trim());
    }
    if (status) {
      params = params.set('status', status.trim());
    }
    

    return this.http.get<{ data: Documento[] }>(this.apiUrlListar, { params }).pipe(
      map((response) => {
        if (response.data && Array.isArray(response.data)) {
          return response.data.map((doc) => ({
            ...doc,
            possuiArquivoPdf: doc['possuiArquivoPdf'] || false,
            qrcode: doc['qrcode'] || '',
            endereco: doc['endereco'] || '',
          }));
        }

        console.warn('Estrutura inesperada na resposta da API:', response);
        return [];
      }),
      catchError((error) => {
        console.error('Erro ao buscar documentos:', error);
        return throwError(() => new Error('Erro ao buscar documentos.'));
      })
    );
  }

  uploadDocumento(formData: FormData): Observable<Documento> {
    return this.http.post<Documento>(this.apiUrlProcessar, formData, {
      headers: {
        Accept: 'application/json',
      },
    });
  }

  downloadDocumento(documentoId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrlListar}/${documentoId}/download`, {
      responseType: 'blob',
    }).pipe(
      catchError((error: { status: number; message: any }) => {
        if (error.status === 404) {
          console.error('Arquivo não encontrado para download:', error.message);
          return throwError(() => new Error('Arquivo não disponível para download.'));
        }
        console.error('Erro ao baixar documento:', error);
        return throwError(() => new Error('Erro ao baixar documento.'));
      })
    );
  }
}
