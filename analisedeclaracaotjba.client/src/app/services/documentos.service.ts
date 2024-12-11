import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { Documento } from '../models/documento.model';

@Injectable({
  providedIn: 'root',
})
export class DocumentosService {
  private apiUrlListar = '/api/documentos-list'; // URL base para a API
  private apiUrlProcessar = '/api/processar-pdf'; // URL para upload

  constructor(private http: HttpClient) { }

  getDocumentos(
    filtro?: string,
    status?: string,
    cpfCnpj?: string,
    certidaoNumero?: string
  ): Observable<Documento[]> {
    let url = this.apiUrlListar;

    if (filtro || status || cpfCnpj || certidaoNumero) {
      const params = new HttpParams()
        .set('filtro', filtro || '')
        .set('status', status || '')
        .set('cpfCnpj', cpfCnpj || '')
        .set('certidaoNumero', certidaoNumero || '');
      url += `?${params.toString()}`;
    }

    return this.http.get<{ data: Documento[] | { data: Documento[] } }>(url).pipe(
      map((response) => {
        if (Array.isArray(response.data)) {
          // Caso o "data" seja o array diretamente
          return response.data.map((doc) => ({
            ...doc,
            possuiArquivoPdf: doc['possuiArquivoPdf'] || false,
          }));
        } else if (response.data && Array.isArray(response.data.data)) {
          // Caso o "data" contenha outro "data"
          return response.data.data.map((doc) => ({
            ...doc,
            possuiArquivoPdf: doc['possuiArquivoPdf'] || false,
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
      catchError((error) => {
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
