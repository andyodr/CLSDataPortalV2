import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TargetComponent } from './target/target.component';

const routes: Routes = [
  {
    path: '',
    component: TargetComponent,
    children: []
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TargetRoutingModule { }
