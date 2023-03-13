import { Component, OnInit, ViewChild } from "@angular/core"
import { ProgressBarMode } from "@angular/material/progress-bar"
import { MatSort } from "@angular/material/sort"
import { MatTableDataSource } from "@angular/material/table"
import { Subscription } from "rxjs"
import { processError } from "../app-constants"
import { Hierarchy, HierarchyApiResult } from "../_models/regionhierarchy"
import { HierarchyService } from "../_services/hierarchy.service"
import { LoggerService } from "../_services/logger.service"

@Component({
    selector: "app-hierarchy",
    templateUrl: "./hierarchy.component.html",
    styleUrls: ["./hierarchy.component.scss"]
})
export class RegionHierarchyComponent implements OnInit {
    title = "Region Hierarchy"
    dataSource = new MatTableDataSource([] as Hierarchy[])
    displayedColumns = ["name", "parentName", "level", "active"]
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

    constructor(private hierarchyService: HierarchyService, public logger: LoggerService) { }

    ngOnDestroy(): void {
        this.userSubscription.unsubscribe()
    }

    ngOnInit(): void {
        this.userSubscription = this.hierarchyService.getHierarchy().subscribe({
            next: (response: any) => {
                this.dataSource = new MatTableDataSource((response as HierarchyApiResult).data)
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
