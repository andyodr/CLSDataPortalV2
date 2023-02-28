import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { ToastrModule } from 'ngx-toastr';
import { NavbarComponent } from './navbar/navbar.component';
import { RouterModule } from '@angular/router';

@NgModule({
  declarations: [
    NavbarComponent
  ],
  imports: [
    RouterModule,
    CommonModule,
    BsDropdownModule.forRoot(),
    ToastrModule.forRoot({
      positionClass: 'toast-bottom-right'
    })
  ],
  exports: [ // This is the key to making the modules available to other modules
    BsDropdownModule,
    ToastrModule,
    NavbarComponent
  ]
})
export class SharedModule { }
