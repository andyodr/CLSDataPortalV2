import { Component, OnInit } from "@angular/core"
import { ActivatedRoute } from "@angular/router"
import { RegionFilter } from "../_models/regionfilter"
import { UserRole } from "../_models/user"
import { LoggerService } from "../_services/logger.service"
import { UserService } from "../_services/user.service"

@Component({
    selector: "app-useredit",
    templateUrl: "./useredit.component.html",
    styleUrls: ["./useredit.component.scss"]
})
export class UserEditComponent implements OnInit {
    title = "Edit User"
    roles: UserRole[] = []
    hierarchy: RegionFilter[] = []
    disabledAll = false
    model = {
        id: -1,
        userName: "",
        firstName: "",
        lastName: "",
        roleId: 1,
        department: "",
        active: false,
        selectedRegions: [] as number[]
    }

    constructor(private route: ActivatedRoute, private userService: UserService, private logger: LoggerService) { }

    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            const id = Number(params.get("id"))
            this.userService.getUserData(id).subscribe(ud => {
                const { userName, firstName, lastName, roleId, department, active, hierarchiesId } = ud.data[0]
                this.roles = ud.roles
                this.hierarchy = ud.hierarchy
                this.model = {
                    id,
                    userName,
                    roleId,
                    firstName: firstName ?? "",
                    lastName: lastName ?? "",
                    department: department ?? "",
                    active: active === "true",
                    selectedRegions: hierarchiesId
                }
            })
        })
    }

    save() {
        const { id, userName, firstName, lastName, roleId, department, active } = this.model
        this.userService.updateUser({
            id,
            userName,
            firstName,
            lastName,
            roleId,
            department,
            active: active.toString(),
            hierarchiesId: this.model.selectedRegions
        }).subscribe(ud => {
            this.logger.logSuccess("User information saved")
        })
    }
}
