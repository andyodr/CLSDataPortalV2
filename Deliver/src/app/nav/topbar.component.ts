import { Component } from "@angular/core"
import { AccountService } from "src/app/_services/account.service"
import { NavSettingsService } from "src/app/_services/nav-settings.service"
import { ToggleService } from "../_services/toggle.service"
import { AsyncPipe } from "@angular/common"
import { RouterLink } from "@angular/router"
import { MatMenuModule } from "@angular/material/menu"
import { MatButtonModule } from "@angular/material/button"
import { MatIconModule } from "@angular/material/icon"

@Component({
    selector: "app-topbar",
    templateUrl: "./topbar.component.html",
    styleUrls: ["./topbar.component.css"],
    standalone: true,
    imports: [MatIconModule, MatButtonModule, MatMenuModule, RouterLink, AsyncPipe]
})
export class TopbarComponent {
    readonly expanded = this.toggleService.toggle

    constructor(public acctSvc: AccountService,
        private toggleService: ToggleService,
        public _navSettingsService: NavSettingsService) { }

    logout() {
        this.acctSvc.logout()
    }

    toggle() {
        this.toggleService.toggle.set(!this.expanded())
    }
}
