import { Component, OnInit } from "@angular/core"
import { LoggerService } from "../_services/logger.service"

@Component({
    selector: "app-useradd",
    templateUrl: "./useradd.component.html",
    styleUrls: ["./useradd.component.scss"]
})
export class UserAddComponent implements OnInit {
    title = "Add User"
    constructor(private logger: LoggerService) { }

    ngOnInit(): void {
        this.logger.logError("Not Implemented")
    }

}
