import { Component, OnInit } from "@angular/core"
import { ToggleService } from "../_services/toggle.service"
import { AccountService } from "../_services/account.service"

@Component({
    selector: "app-sidebar",
    templateUrl: "./sidebar.component.html",
    styleUrls: ["./sidebar.component.scss"]
})
export class SidebarComponent implements OnInit {
    opened = true
    roleId = 0

    constructor(public api: AccountService, private toggleService: ToggleService) { }

    ngOnInit(): void {
        this.api.currentUser$.subscribe(u => this.roleId = u?.roleId ?? 0)
        this.toggleService.toggle$.subscribe(toggle => this.opened = toggle)
    }

    tableau() {
        0
    }
}
