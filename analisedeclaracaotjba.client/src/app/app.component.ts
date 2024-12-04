import { Component } from '@angular/core';

@Component({
  selector: 'app-root', // O seletor usado no index.html
  template: `
    <div>
      <h1>Bem-vindo ao Sistema de Análise de Declarações</h1>
      <app-documentos></app-documentos> <!-- Renderiza o DocumentosComponent -->
    </div>
  `,
  styleUrls: ['./app.component.css']
})
export class AppComponent { }
