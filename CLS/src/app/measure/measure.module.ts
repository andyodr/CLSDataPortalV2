import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MeasureRoutingModule } from './measure-routing.module';
import { MeasureComponent } from './measure/measure.component';
import { MeasureListComponent } from './measure-list/measure-list.component';
import { SharedModule } from '../_modules/shared.module';


@NgModule({
  declarations: [
    MeasureComponent,
    MeasureListComponent
  ],
  imports: [
    CommonModule,
    MeasureRoutingModule,
    SharedModule
  ]
})
export class MeasureModule { }
