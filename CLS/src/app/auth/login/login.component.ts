import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { NavSettingsService } from 'src/app/_services/nav-settings.service';
import { AccountService } from '../../_services/account.service';
import { LoggerService } from '../../_services/logger.service';


@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  @Output() cancelLogin = new EventEmitter();
  model: any = {};


    constructor(
        private accountService: AccountService,
        private router: Router,
        private logger: LoggerService,
        public _navSettingsService: NavSettingsService) { }

  ngOnInit(): void {
    this._navSettingsService.hideNavBar();
  }

  /*register() {
    this.accountService.register(this.model).subscribe({
      next: () => {
        this.cancel();
      },
      error: error => {
        this.toastr.error(error.error)
        console.log(error);
      }
    });
  }

  cancel() {  
    this.cancelRegister.emit(false);
  } */

  login() {
    console.log(this.model);
    this.accountService.login(this.model).subscribe({
      next: _ => {
        this._navSettingsService.showNavBar();
        this.router.navigateByUrl('/measuredata');
      },
      error: error => this.logger.logError(error.message)
    });
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

  cancel() {  
    this.cancelLogin.emit(false);
  }
}
