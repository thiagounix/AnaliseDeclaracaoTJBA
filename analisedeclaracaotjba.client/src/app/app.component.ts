import { Component } from '@angular/core';

@Component({
  selector: 'app-root', // O seletor usado no index.html
  template: `
    <div class="app-container">
      <header>
        <h1>Sistema de Análise de Declarações</h1>
        <nav>
          <a routerLink="/" routerLinkActive="active">Início</a>
          <a routerLink="/documentos-list" routerLinkActive="active">Listar Documentos</a>
          <a routerLink="/documentos-upload" routerLinkActive="active">Enviar Documento</a>
        </nav>
      </header>
      <main>
        <router-outlet></router-outlet> <!-- Renderiza os componentes baseados nas rotas -->
      </main>
      <footer>
        <p>&copy; 2024 Sistema de Análise de Declarações</p>
      </footer>
    </div>
  `,
  styleUrls: ['./app.component.css']
})
export class AppComponent { }
