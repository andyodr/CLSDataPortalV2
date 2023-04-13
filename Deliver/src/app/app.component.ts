import { Component, OnInit } from '@angular/core';
import { UserState } from "./_models/user"
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
		const userString = localStorage.getItem("userState")
		if (!userString) return
		const userState = JSON.parse(userString!) as UserState
        if (!userState) return
		this.api.setCurrentUser(userState)
	}

	getCurrentUser() {
		return this.api.getCurrentUser();
	}
}
