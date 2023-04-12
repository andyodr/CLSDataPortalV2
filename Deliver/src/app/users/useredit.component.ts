import { Component, OnInit } from "@angular/core"
import { ActivatedRoute, Router } from "@angular/router"
import { RegionFilter } from "../_services/hierarchy.service"
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
        selectedRegions: [] as number | number[] | null
    }

    constructor(private router: Router, private route: ActivatedRoute,
        private api: UserService, private logger: LoggerService) { }

    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            const id = Number(params.get("id"))
            this.api.getUserData(id).subscribe(ud => {
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
                    active: active ?? false,
                    selectedRegions: hierarchiesId
                }
            })
        })
    }

    save() {
        const { id, userName, firstName, lastName, roleId, department, active, selectedRegions: hierarchiesId } = this.model
        if (Array.isArray(hierarchiesId)) {
            this.api.updateUser({
                id,
                userName,
                firstName,
                lastName,
                roleId,
                department,
                active: active,
                hierarchiesId
            }).subscribe({
                next: result => {
                    this.logger.logSuccess(`Saved UserId ${ result.data[0].id }`)
                    setTimeout(() => this.router.navigate(["users"]), 500)
                },
                error: () => this.logger.logError("Error")
            })
        }
    }
}
