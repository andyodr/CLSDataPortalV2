import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MeasureDefinitionRoutingModule } from './measure-definition-routing.module';
import { MeasureDefinitionComponent } from './measure-definition/measure-definition.component';
import { MeasureDefinitionListComponent } from './measure-definition-list/measure-definition-list.component';
import { SharedModule } from '../_modules/shared.module';


@NgModule({
  declarations: [
    MeasureDefinitionComponent,
    MeasureDefinitionListComponent
  ],
  imports: [
    CommonModule,
    MeasureDefinitionRoutingModule,
    SharedModule
  ]
})
export class MeasureDefinitionModule { }
