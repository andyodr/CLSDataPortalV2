import { Component } from "@angular/core"
import { AccountService } from "src/app/_services/account.service"
import { NavSettingsService } from "src/app/_services/nav-settings.service"
import { ToggleService } from "../_services/toggle.service"

@Component({
    selector: "app-topbar",
    templateUrl: "./topbar.component.html",
    styleUrls: ["./topbar.component.css"]
})
export class TopbarComponent {
    public opened = true

    constructor(public api: AccountService,
        private toggleService: ToggleService,
        public _navSettingsService: NavSettingsService) { }

    logout() {
        this.api.logout()
    }

    toggle() {
        this.opened = !this.opened;
        this.toggleService.setToggle(this.opened)
    }
}
