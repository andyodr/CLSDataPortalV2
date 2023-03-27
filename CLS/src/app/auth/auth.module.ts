import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthRoutingModule } from './auth-routing.module';
import { LoginComponent } from './login/login.component';
import { MatTooltipModule } from '@angular/material/tooltip';

@NgModule({
  declarations: [LoginComponent],
  imports: [
    CommonModule,
    FormsModule,
    MatTooltipModule,
    AuthRoutingModule
  ],
  exports: [LoginComponent]
})

export class AuthModule { }
