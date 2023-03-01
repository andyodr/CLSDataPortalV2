import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HierarchyRoutingModule } from './hierarchy-routing.module';
import { HierarchyComponent } from './hierarchy/hierarchy.component';
import { HierarchyListComponent } from './hierarchy-list/hierarchy-list.component';
import { SharedModule } from '../_modules/shared.module';


@NgModule({
  declarations: [
    HierarchyComponent,
    HierarchyListComponent
  ],
  imports: [
    CommonModule,
    HierarchyRoutingModule,
    SharedModule
  ]
})
export class HierarchyModule { }
