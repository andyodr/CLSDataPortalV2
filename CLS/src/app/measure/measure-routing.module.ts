import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MeasureComponent } from './measure/measure.component';

const routes: Routes = [
  {
    path: '',
    component: MeasureComponent,
    children: []
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MeasureRoutingModule { }
