import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { DocumentosListComponent } from './components/documentos-list/documentos-list.component';
import { DocumentosUploadComponent } from './components/documentos-upload/documentos-upload.component';
import { AppRoutingModule } from './app-routing.module'; // Import do módulo de rotas
import { HomeComponent } from './components/home/home.component'; // Import do HomeComponent

@NgModule({
  declarations: [
    AppComponent,
    DocumentosListComponent,
    DocumentosUploadComponent,
    HomeComponent 
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpClientModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
