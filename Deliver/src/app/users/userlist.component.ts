import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core"
import { MatSort } from "@angular/material/sort"
import { MatTableDataSource } from "@angular/material/table"
import { Router } from "@angular/router"
import { Subscription } from "rxjs"
import { User, UserData } from "../_models/user"
import { UserService } from "../_services/user.service"
import { MSG_DATA_NO_FOUND, MSG_ERROR_PROCESSING, processError } from "../lib/app-constants"
import { NavigationService } from "../_services/nav.service"

@Component({
    selector: "app-user-list",
    templateUrl: "./userlist.component.html",
    styleUrls: ["./userlist.component.scss"]
})
export class UserListComponent implements OnInit, OnDestroy {
    title = "Users"
    dataSource = new MatTableDataSource<User>()
    displayedColumns = ["userName", "lastName", "firstName", "department", "roleName", "active"]
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
