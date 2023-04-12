import { Component, OnInit } from '@angular/core';
import { AuthenticatedUser } from './_models/user';
import { AccountService } from './_services/account.service';
import { NavSettingsService } from './_services/nav-settings.service';
import { ToggleService } from './_services/toggle.service';

@Component({
	selector: 'app-root',
	templateUrl: './app.component.html',
	styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
	toggle: any = true;

	constructor(private api: AccountService, private toggleService: ToggleService, public _navSettingsService: NavSettingsService) {
	 }

	ngOnInit(): void {
		this.setCurrentUser();
		this.toggleService.toggle$.subscribe(toggle => {
			this.toggle = toggle;
		});
	}

	setCurrentUser() {
		const userString = localStorage.getItem('user')
		if (!userString) return
		const user: AuthenticatedUser = JSON.parse(userString!)
        if (!user) return
		this.api.setCurrentUser(user)
	}

	getCurrentUser() {
		return this.api.getCurrentUser();
	}
}
