import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AccountService } from 'src/app/_services/account.service';
import { NavSettingsService } from 'src/app/_services/nav-settings.service';
import { ToggleService } from '../../_services/toggle.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {

  public  opened = true;
  model: any = {};

  constructor(public accountService: AccountService , private router: Router, 
              private toogleService: ToggleService, public _navSettingsService: NavSettingsService) { }

  ngOnInit(): void {
  }

  login() {
    this.accountService.login(this.model).subscribe({
      next: _ => this.router.navigateByUrl('/users')//,
      //error: error => this.toastr.error(error.error)
    });
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

  toggle() {
    this.opened = !this.opened;
    this.toogleService.setToggle(this.opened);
  }
}
