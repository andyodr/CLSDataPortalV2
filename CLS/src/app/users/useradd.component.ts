import { Component, OnInit } from "@angular/core"

@Component({
    selector: "app-useradd",
    templateUrl: "./useradd.component.html",
    styleUrls: ["./useradd.component.scss"]
})
export class UserAddComponent implements OnInit {
    title = "Add User"
    constructor() { }

    ngOnInit(): void {
    }

}
