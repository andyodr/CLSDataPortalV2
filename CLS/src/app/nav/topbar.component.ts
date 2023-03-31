import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AccountService } from 'src/app/_services/account.service';
import { NavSettingsService } from 'src/app/_services/nav-settings.service';
import { ToggleService } from '../_services/toggle.service';

@Component({
    selector: 'app-topbar',
    templateUrl: './topbar.component.html',
    styleUrls: ['./topbar.component.css']
})
export class NavbarComponent {
    public opened = true;
    model: any = {};

    constructor(public api: AccountService, private router: Router,
        private toogleService: ToggleService, public _navSettingsService: NavSettingsService) { }

    login() {
        this.api.login(this.model).subscribe({
            next: _ => this.router.navigateByUrl('/users')//,
        });
    }

    logout() {
        this.api.logout();
        this.router.navigateByUrl('/');
    }

    toggle() {
        this.opened = !this.opened;
        this.toogleService.setToggle(this.opened);
    }
}
