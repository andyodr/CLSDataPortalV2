import { Component, OnInit, ViewChild } from "@angular/core"
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
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = true
    drawerTitle = "Add"
    hierarchyLevels!: { name: string, id: number }[]
    model = {
        id: 0,
        active: false,
        name: "",
        level: 0
    }

    constructor(private hierarchyService: HierarchyService, public logger: LoggerService) { }

    ngOnDestroy(): void {
        this.userSubscription.unsubscribe()
    }

    ngOnInit(): void {
        this.userSubscription = this.hierarchyService.getHierarchy().subscribe({
            next: (response: any) => {
                this.dataSource = new MatTableDataSource((response as HierarchyApiResult).data)
                this.hierarchyLevels = (response as HierarchyApiResult).levels
                this.dataSource.sort = this.sort
                // processLocalError here
            },
            error: (err: any) => {
                this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
            }
        })
    }

    add() {
        this.drawerTitle = "Add"
        this.model.id = -1
        this.model.active = false
        this.model.name = ""
        this.model.level = 0
    }

    edit(hid: number) {
        this.drawerTitle = "Edit"
        let hierarchy = this.dataSource.data.find(h => h.id == hid)
        this.model.id = hierarchy?.id ?? 0
        this.model.active = hierarchy?.active ?? false
        this.model.name = hierarchy?.name ?? ""
        this.model.level = hierarchy?.levelId ?? 0
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
