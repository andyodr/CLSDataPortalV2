import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TargetRoutingModule } from './target-routing.module';
import { TargetComponent } from './target/target.component';
import { TargetListComponent } from './target-list/target-list.component';
import { SharedModule } from '../_modules/shared.module';


@NgModule({
  declarations: [
    TargetComponent,
    TargetListComponent
  ],
  imports: [
    CommonModule,
    TargetRoutingModule,
    SharedModule
  ]
})
export class TargetModule { }
