import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms'; // Certifique-se de que está importado
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { DocumentosComponent } from './components/documentos.component';

@NgModule({
  declarations: [
    AppComponent,
    DocumentosComponent
  ],
  imports: [
    BrowserModule,
    FormsModule, // Incluído para suportar [(ngModel)]
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
