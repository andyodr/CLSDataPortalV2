import { Component, OnInit } from "@angular/core"
import { RegionFilter, RegionFlatNode } from "../_models/regionfilter"
import { UserRole } from "../_models/user"
import { LoggerService } from "../_services/logger.service"
import { UserService } from "../_services/user.service"

@Component({
    selector: "app-useradd",
    templateUrl: "./useradd.component.html",
    styleUrls: ["./useradd.component.scss"]
})
export class UserAddComponent implements OnInit {
    title = "Add User"
    roles: UserRole[] = []
    hierarchy: RegionFilter[] = []
    disabledAll = false
    model = {
        userName: "",
        firstName: "",
        lastName: "",
        roleId: 1,
        department: "",
        active: false,
        selectedRegions: [] as number[]
    }

    constructor(private userService: UserService, private logger: LoggerService) { }

    ngOnInit(): void {
        this.userService.getRolesAndRegions().subscribe(r => {
            this.roles = r.roles
            this.hierarchy = r.hierarchy
        })
    }

    save() {
        const { userName, firstName, lastName, roleId, department, active, selectedRegions } = this.model
        this.userService.addUser({
            userName, firstName, lastName, roleId, department,
            active: active.toString(),
            hierarchiesId: selectedRegions
        }).subscribe(u => this.logger.logInfo(`New user id: ${u.data[0].id}`))
    }

    refresh() {
        console.log(this.model)
        this.logger.logWarning("Not Implemented")
    }
}
