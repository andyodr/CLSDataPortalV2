import { Component, OnInit } from "@angular/core"
import { RegionFilter } from "../_services/hierarchy.service"
import { UserRole } from "../_models/user"
import { LoggerService } from "../_services/logger.service"
import { UserService } from "../_services/user.service"
import { RegionTreeComponent } from "../lib/region-tree/region-tree.component"
import { MatCheckboxModule } from "@angular/material/checkbox"
import { MatOptionModule } from "@angular/material/core"
import { MatSelectModule } from "@angular/material/select"
import { MatInputModule } from "@angular/material/input"
import { MatFormFieldModule } from "@angular/material/form-field"
import { FormsModule } from "@angular/forms"
import { MatIconModule } from "@angular/material/icon"
import { NavigateBackDirective } from "../_services/nav.service"
import { MatButtonModule } from "@angular/material/button"
import { SidebarComponent } from "../nav/sidebar.component"

@Component({
    selector: "app-useradd",
    templateUrl: "./useradd.component.html",
    styleUrls: ["./useradd.component.scss"],
    standalone: true,
    imports: [SidebarComponent, MatButtonModule, NavigateBackDirective, MatIconModule, FormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatOptionModule, MatCheckboxModule, RegionTreeComponent]
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
        selectedRegions: [] as number | number[] | null
    }

    constructor(private api: UserService, private logger: LoggerService) { }

    ngOnInit(): void {
        this.api.getRolesAndRegions().subscribe(r => {
            this.roles = r.roles
            this.hierarchy = r.hierarchy
        })
    }

    save() {
        const { userName, firstName, lastName, roleId, department, active, selectedRegions: hierarchiesId } = this.model
        if (Array.isArray(hierarchiesId)) {
            this.api.addUser({
                userName, firstName, lastName, roleId, department,
                active: active,
                hierarchiesId
            }).subscribe(u => this.logger.logInfo(`New user id: ${u.data[0].id}`))
        }
    }

    refresh() {
        console.log(this.model)
        this.logger.logWarning("Not Implemented")
    }
}
