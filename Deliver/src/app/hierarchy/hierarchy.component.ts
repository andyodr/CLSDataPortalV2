import { Component, DestroyRef, OnInit, ViewChild, inject } from "@angular/core"
import { MatSort, MatSortModule } from "@angular/material/sort"
import { MatTableDataSource, MatTableModule } from "@angular/material/table"
import { finalize } from "rxjs"
import { processError } from "../lib/app-constants"
import { Hierarchy, RegionFilter } from "../_services/hierarchy.service"
import { HierarchyService } from "../_services/hierarchy.service"
import { LoggerService } from "../_services/logger.service"
import { NgbAlert } from "@ng-bootstrap/ng-bootstrap"
import { ErrorsComponent } from "../errors/errors.component"
import { SidebarComponent } from "../nav/sidebar.component"
import { RegionTreeComponent } from "../lib/region-tree/region-tree.component"
import { MatOptionModule } from "@angular/material/core"
import { MatSelectModule } from "@angular/material/select"
import { MatInputModule } from "@angular/material/input"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatCheckboxModule } from "@angular/material/checkbox"
import { FormsModule } from "@angular/forms"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatSidenavModule } from "@angular/material/sidenav"
import { MatProgressBarModule } from "@angular/material/progress-bar"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"

@Component({
    selector: "app-hierarchy",
    templateUrl: "./hierarchy.component.html",
    styleUrls: ["./hierarchy.component.scss"],
    standalone: true,
    imports: [MatProgressBarModule, MatSidenavModule, MatButtonModule, MatIconModule, FormsModule, MatCheckboxModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatOptionModule, RegionTreeComponent, SidebarComponent, ErrorsComponent, NgbAlert, MatTableModule, MatSortModule]
})
export class RegionHierarchyComponent implements OnInit {
    title = "Region Hierarchy"
    dataSource = new MatTableDataSource<Hierarchy>()
    displayedColumns = ["id", "name", "parentName", "level"]
    @ViewChild(MatSort) sort!: MatSort
    destroyRef = inject(DestroyRef)
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = true
    drawerTitle = "Add"
    progress = false
    hierarchy: RegionFilter[] = []
    hierarchyLevels!: { name: string, id: number }[]
    model = {
        id: 0,
        active: false,
        name: "",
        level: 0,
        selectedParent: null as number | number[] | null
    }

    constructor(private api: HierarchyService, public logger: LoggerService) { }

    ngOnInit(): void {
        this.progress = true
        this.api.getHierarchy()
            .pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.dataSource = new MatTableDataSource(response.data)
                    this.hierarchyLevels = response.levels
                    this.hierarchy = response.hierarchy
                    this.dataSource.sort = this.sort
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
        this.model.selectedParent = null
    }

    edit(hid: number) {
        this.drawerTitle = "Edit"
        const region = this.dataSource.data.find(h => h.id == hid)
        this.model.id = region?.id ?? 0
        this.model.active = region?.active ?? false
        this.model.name = region?.name ?? ""
        this.model.level = region?.levelId ?? 0
        this.model.selectedParent = region?.parentId ?? null
    }

    save() {
        const { id, name, active, level: levelId, selectedParent: parentId } = this.model
        if (typeof parentId !== "number" || id === parentId) {
            this.logger.logWarning(`The parentId: ${ parentId } is not valid here`)
            return
        }

        if (this.drawerTitle === "Add") {
            var op = this.api.addHierarchy({ levelId, name, parentId, active })
        }
        else {
            var op = this.api.updateHierarchy({ id, levelId, name, parentId, active })
        }

        this.progress = true
        op.pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: _ => {
                    this.logger.logSuccess("Save completed")
                    this.ngOnInit()
                }
            })
    }

    applyFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
    }

    identity(_: number, item: { id: number }) {
        return item.id
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
