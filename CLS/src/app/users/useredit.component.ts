import { Component, OnInit } from "@angular/core"
import { ActivatedRoute } from "@angular/router";
import { UserData } from "../_models/user";
import { UserService } from "../_services/user.service";

type Model = {
    userName: string
    roleId: number
    firstName: string
    lastName: string
    department: string
    active: boolean
}

@Component({
    selector: "app-useredit",
    templateUrl: "./useredit.component.html",
    styleUrls: ["./useredit.component.scss"]
})
export class UserEditComponent implements OnInit {
    title = "Edit User"
    userData!: UserData
    model!: Model
    disabledAll = false
    constructor(private route: ActivatedRoute, private userService: UserService) { }

    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            let id = Number(params.get("id"))
            this.userService.getUser(id).subscribe(user => {
                this.userData = user
                this.model = {
                    userName: user.data[0].userName,
                    roleId: user.data[0].roleId,
                    firstName: user.data[0].firstName ?? "",
                    lastName: user.data[0].lastName ?? "",
                    department: user.data[0].department ?? "",
                    active: user.data[0].active === "true"
                }
            })
        })
    }
}
