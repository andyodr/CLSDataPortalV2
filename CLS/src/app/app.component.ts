import { Component, OnInit } from '@angular/core';
import { User } from './_models/user';
import { AccountService } from './_services/account.service';
import { ToggleService } from './_services/toggle.service';

@Component({
	selector: 'app-root',
	templateUrl: './app.component.html',
	styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
	title = 'CLSDataPortalV2';

	toggle: any = true;

	constructor(private accountService: AccountService, private toggleService: ToggleService) { }

	ngOnInit(): void {
		this.setCurrentUser();
		this.toggleService.toggle$.subscribe(toggle => {
			this.toggle = toggle;
		});
	}

	setCurrentUser() {
		//const user: User = JSON.parse(localStorage.getItem('user')!);
		const userString = localStorage.getItem('user');
		if (!userString) return;
		const user: User = JSON.parse(userString!);
		this.accountService.setCurrentUser(user);
	}
}
