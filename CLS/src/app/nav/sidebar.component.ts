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
    admin2 = this.api.currentUser$.subscribe(u => u && u.roleId > 2)
    admin3 = this.api.currentUser$.subscribe(u => u && u.roleId > 3)

    constructor(public api: AccountService, private toggleService: ToggleService) { }

    ngOnInit(): void {
        this.toggleService.toggle$.subscribe(toggle => {
            this.opened = toggle
        })
    }
}
