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

  getDocumentos(filtro?: string, status?: string, cpfCnpj?: string, certidaoNumero?: string, ): Observable<Documento[]> {
        if (filtro || status || cpfCnpj || certidaoNumero ) {
      const params = new HttpParams()
        .set('filtro', filtro || '')
        .set('status', status || '')
        .set('cpfCnpj', cpfCnpj || '')
        .set('certidaoNumero', certidaoNumero || '');
      this.apiUrlListar += `?${params.toString()}`;
    }

    return this.http.get<{ data: Documento[] }>(this.apiUrlListar).pipe(
      map(response => response.data),
      catchError(error => {
        if (error.status === 404) {
          console.warn('Nenhum documento encontrado:', error.message);
          return of([]); // Retorna uma lista vazia em caso de 404
        }
        console.error('Erro ao buscar documentos:', error);
        return throwError(() => new Error('Erro ao buscar documentos.'));
      })
    );
  }


  uploadDocumento(formData: FormData): Observable<Documento> {
    return this.http.post<Documento>(this.apiUrlProcessar, formData, {
      headers: {
        'Accept': 'application/json',
      },
    });
  }

  // Baixa um documento específico
  downloadDocumento(documentoId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrlListar}/${documentoId}/download`, {
      responseType: 'blob',
    });
  }
}
