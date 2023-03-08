import { Component, OnInit } from "@angular/core"
import { ActivatedRoute } from "@angular/router";

@Component({
    selector: "app-useredit",
    templateUrl: "./useredit.component.html",
    styleUrls: ["./useredit.component.scss"]
})
export class UserEditComponent implements OnInit {
    id!: number
    constructor(private route: ActivatedRoute) { }

    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            this.id = Number(params.get("id"))
        })
    }
}
