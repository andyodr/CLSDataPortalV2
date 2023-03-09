import { Component, OnInit, OnDestroy, ViewChild } from "@angular/core"
import { ProgressBarMode } from "@angular/material/progress-bar"
import { MatSort } from "@angular/material/sort"
import { MatTableDataSource } from "@angular/material/table"
import { Router } from "@angular/router"
import { Subscription } from "rxjs"
import { User, UserData } from "src/app/_models/user"
import { UserService } from "src/app/_services/user.service"
import { MSG_DATA_NO_FOUND, MSG_ERROR_PROCESSING, processError } from "../app-constants"
import { NavigationService } from "../_services/nav.service"

@Component({
    selector: "app-user-list",
    templateUrl: "./userlist.component.html",
    styleUrls: ["./userlist.component.scss"]
})
export class UserListComponent implements OnInit, OnDestroy {
    title = "Users"
    dataSource = new MatTableDataSource([] as User[])
    displayedColumns = ["userName", "lastName", "firstName", "department", "roleName", "active"]
    @ViewChild(MatSort) sort!: MatSort
    private userSubscription = new Subscription()
    toggle: any = true
    disabledAll = false
    skUsers = ""
    errorMsg: any = ""
    showError = false
    showContentPage = true
    progress = {
        mode: "determinate" as ProgressBarMode,
        value: 0
    }

    constructor(private userService: UserService, public router: Router, private _: NavigationService) { }

    ngOnDestroy(): void {
        this.userSubscription.unsubscribe()
    }

    ngOnInit(): void {
        this.userSubscription = this.userService.getUsers().subscribe({
            next: (response: any) => {
                this.dataSource = new MatTableDataSource((response as UserData).data)
                // processLocalError here
            },
            error: (err: any) => {
                this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
            }
        })
    }

    ngAfterViewInit(): void {
        this.dataSource.sort = this.sort
    }

    applyFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
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
