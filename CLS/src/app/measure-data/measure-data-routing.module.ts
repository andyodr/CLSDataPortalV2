import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeasureDataComponent } from './measuredata/measuredata.component';

const routes: Routes = [
  { 
    path: '', 
    component: MeasureDataComponent,
    children: []
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MeasureDataRoutingModule { }
