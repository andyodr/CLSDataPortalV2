import { Component, OnInit } from "@angular/core"
import { ToggleService } from "../_services/toggle.service"

@Component({
    selector: "app-sidebar",
    templateUrl: "./sidebar.component.html",
    styleUrls: ["./sidebar.component.scss"]
})
export class SidebarComponent implements OnInit {
    public opened = true

    constructor(private toggleService: ToggleService) { }

    ngOnInit(): void {
        this.toggleService.toggle$.subscribe(toggle => {
            this.opened = toggle
        })
    }
}
