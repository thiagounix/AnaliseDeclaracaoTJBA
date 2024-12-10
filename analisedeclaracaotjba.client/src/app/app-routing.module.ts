import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DocumentosListComponent } from './components/documentos-list/documentos-list.component';
import { DocumentosUploadComponent } from './components/documentos-upload/documentos-upload.component';
import { HomeComponent } from './components/home/home.component';
const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'documentos-list', component: DocumentosListComponent },
  { path: 'documentos-upload', component: DocumentosUploadComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
