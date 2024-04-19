import { Component } from "@angular/core"
import { ToggleService } from "../_services/toggle.service"
import { AccountService } from "../_services/account.service"
import { RouterLink } from "@angular/router"
import { MatRippleModule } from "@angular/material/core"
import { NgClass } from "@angular/common"
import { MatIconModule } from "@angular/material/icon"

@Component({
    selector: "app-sidebar",
    templateUrl: "./sidebar.component.html",
    styleUrls: ["./sidebar.component.scss"],
    standalone: true,
    imports: [NgClass, MatRippleModule, MatIconModule, RouterLink]
})
export class SidebarComponent {
    readonly expanded = this.toggleService.toggle
    roleId = 0

    constructor(public api: AccountService, private toggleService: ToggleService) {
        this.api.currentUser$.subscribe(u => this.roleId = u?.roleId ?? 0)
    }

    tableau() {
        0
    }
}
