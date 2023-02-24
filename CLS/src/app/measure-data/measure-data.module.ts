import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MeasureDataRoutingModule } from './measure-data-routing.module';
import { MeasureDataListComponent } from './measure-data-list/measure-data-list.component';
import { MeasureDataComponent } from './measuredata/measuredata.component';
import { SharedModule } from '../_modules/shared.module';


@NgModule({
  declarations: [
    MeasureDataListComponent,
    MeasureDataComponent
  ],
  imports: [
    CommonModule,
    MeasureDataRoutingModule,
    SharedModule
  ]
})
export class MeasureDataModule { }
