import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from './navbar/navbar.component';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

@NgModule({
  declarations: [
    NavbarComponent
  ],
  imports: [
    RouterModule,
    CommonModule,
    MatIconModule,
    MatMenuModule
  ],
  exports: [ // This is the key to making the modules available to other modules
    NavbarComponent
  ]
})
export class SharedModule { }
