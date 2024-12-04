import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Documento } from '../models/documento.model';

@Injectable({
  providedIn: 'root'
})
export class DocumentosService {
  private apiUrl = 'https://localhost:7022/api/documentos'; // Atualizado para HTTPS

  constructor(private http: HttpClient) { }

  getDocumentos(page: number = 1, pageSize: number = 10): Observable<any> {
    return this.http.get<any>(`/api/documentos?page=${page}&pageSize=${pageSize}`);
  }

  uploadDocumento(formData: FormData): Observable<any> {
    return this.http.post(this.apiUrl, formData);
  }
}
