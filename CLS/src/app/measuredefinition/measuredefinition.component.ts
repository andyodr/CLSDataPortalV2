import { Component, OnInit } from "@angular/core"
import { LoggerService } from "../_services/logger.service"

@Component({
    selector: "app-measuredefinition",
    templateUrl: "./measuredefinition.component.html",
    styleUrls: ["./measuredefinition.component.css"]
})
export class MeasureDefinitionComponent implements OnInit {
    title = "Measure Definition"
    constructor(private logger: LoggerService) { }

    ngOnInit(): void {
    }

    save() {
        this.logger.logInfo("Do your stuff in here")
    }
}
