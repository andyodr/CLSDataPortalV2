import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core"
import { MatSort, MatSortModule } from "@angular/material/sort"
import { MatTableDataSource, MatTableModule } from "@angular/material/table"
import { Router, RouterLink } from "@angular/router"
import { Subscription } from "rxjs"
import { User, UserData } from "../_models/user"
import { UserService } from "../_services/user.service"
import { processError } from "../lib/app-constants"
import { NavigationService } from "../_services/nav.service"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatInputModule } from "@angular/material/input"
import { MatFormFieldModule } from "@angular/material/form-field"
import { NgbAlert } from "@ng-bootstrap/ng-bootstrap"
import { ErrorsComponent } from "../errors/errors.component"
import { SidebarComponent } from "../nav/sidebar.component"

@Component({
    selector: "app-user-list",
    templateUrl: "./userlist.component.html",
    styleUrls: ["./userlist.component.scss"],
    standalone: true,
    imports: [SidebarComponent, ErrorsComponent, NgbAlert, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, RouterLink, MatTableModule, MatSortModule]
})
export class UserListComponent implements OnInit, OnDestroy {
    title = "Users"
    dataSource = new MatTableDataSource<User>()
    displayedColumns = ["userName", "lastName", "firstName", "department", "roleName"]
    @ViewChild(MatSort) sort!: MatSort
    private userSubscription = new Subscription()
    toggle: any = true
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = true

    constructor(private api: UserService, public router: Router, private _: NavigationService) { }

    ngOnDestroy(): void {
        this.userSubscription.unsubscribe()
    }

    ngOnInit(): void {
        this.userSubscription = this.api.getUsers().subscribe({
            next: (response: any) => {
                this.dataSource = new MatTableDataSource((response as UserData).data)
                this.dataSource.sort = this.sort
                // processLocalError here
            },
            error: (err: any) => {
                this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
            }
        })
    }

    applyFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
    }

    identity(index: number, row: User) {
        return row.id
    }

    processLocalError(name: string, message: string, id: any, status: unknown, authError: any) {
        this.errorMsg = processError(name, message, id, status)
        this.showError = true
        this.disabledAll = false
        this.showContentPage = (authError != true)
    }

    closeError() {
        this.errorMsg = ""
        this.showError = false
    }
}
