import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeasureDefinitionComponent } from './measure-definition/measure-definition.component';


const routes: Routes = [
  {
    path: '',
    component: MeasureDefinitionComponent,
    children: []
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MeasureDefinitionRoutingModule { }
