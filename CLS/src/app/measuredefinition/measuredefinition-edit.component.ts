import { Component, OnInit } from "@angular/core"

@Component({
    selector: "app-measuredefinition-edit",
    templateUrl: "./measuredefinition-edit.component.html",
    styleUrls: ["./measuredefinition-edit.component.scss"]
})
export class MeasureDefinitionEditComponent implements OnInit {
    title = "Add Measure Definition"
    roles: { id: any, name: any }[] = []
    hierarchy = []
    disabledAll = false
    model = {
        userName: "",
        firstName: "",
        lastName: "",
        roleId: 1,
        department: "",
        active: false,
        selectedRegions: [] as number | number[] | null
    }
    constructor() { }

    ngOnInit(): void {
    }

    save() { }
}
