import { animate, state, style, transition, trigger } from "@angular/animations"
import { Component, DestroyRef, HostListener, OnInit, Signal, ViewChild, inject } from "@angular/core"
import { MatSort, MatSortModule } from "@angular/material/sort"
import { MatTableDataSource, MatTableModule } from "@angular/material/table"
import { finalize } from "rxjs"
import { AccountService } from "../_services/account.service"
import { LoggerService } from "../_services/logger.service"
import { MeasureDefinition, FilterResponseDto, MeasureDefinitionService, MeasureType } from "../_services/measure-definition.service"
import { RouterLink } from "@angular/router"
import { MatMenuModule } from "@angular/material/menu"
import { ErrorsComponent } from "../errors/errors.component"
import { SidebarComponent } from "../nav/sidebar.component"
import { MatInputModule } from "@angular/material/input"
import { MatOptionModule } from "@angular/material/core"
import { MatSelectModule } from "@angular/material/select"
import { MatFormFieldModule } from "@angular/material/form-field"
import { FormsModule } from "@angular/forms"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatSidenavModule } from "@angular/material/sidenav"
import { MatProgressBarModule } from "@angular/material/progress-bar"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import packageJson from "../../../package.json"

@Component({
    selector: "app-measuredefinition",
    templateUrl: "./measuredefinition.component.html",
    styleUrls: ["./measuredefinition.component.scss"],
    animations: [
        trigger("detailExpand", [
            state("false", style({ height: "0px", minHeight: "0" })),
            state("true", style({ height: "*" })),
            transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
        ])
    ],
    standalone: true,
    imports: [MatProgressBarModule, MatSidenavModule, MatButtonModule, MatIconModule, FormsModule,
        MatFormFieldModule, MatSelectModule, MatOptionModule, MatInputModule, SidebarComponent,
        ErrorsComponent, MatMenuModule, RouterLink, MatTableModule, MatSortModule]
})
export class MeasureDefinitionComponent implements OnInit {
    title = "Measure Definition"
    version: string = packageJson.version
    apiVersion: Signal<string> = this.acctSvc.version
    filters!: FilterResponseDto
    filtersSelected: string[] = []
    dataSource = new MatTableDataSource<MeasureDefinition>()
    displayedColumns = ["id", "name", "varName", "description", "calculated", "interval", "priority"]
    expandDetail = new ToggleQuery()
    @ViewChild(MatSort) sort!: MatSort
    measureTypes: MeasureType[] = []
    selectedMeasureType: MeasureType = { id: 0, name: "" }
    progress = false
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = false
    drawer = {
        title: "Filter",
        filter: true,
        position: "start" as "start" | "end"
    }

    measureTypeInput!: MeasureType
    destroyRef = inject(DestroyRef)

    constructor(private api: MeasureDefinitionService, private acctSvc: AccountService, public logger: LoggerService) { }

    ngOnInit(): void {
        const measureTypeId = this.acctSvc.getCurrentUser()?.filter.measureTypeId
        this.progress = true
        this.api.getMeasureDefinitionFilter()
            .pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: filters => {
                    this.filters = filters
                    this.measureTypes = filters.measureTypes
                    const selectedMeasureType = this.measureTypes.find(t => t.id == measureTypeId)
                    this.selectedMeasureType = selectedMeasureType ?? this.measureTypes[0]
                    this.dataSource.sort = this.sort
                    this.loadTable()
                }
            })
    }

    loadTable() {
        this.filtersSelected = [this.selectedMeasureType.name]
        this.acctSvc.saveSettings({ measureTypeId: this.selectedMeasureType.id })
        this.progress = true
        this.api.getMeasureDefinition(this.selectedMeasureType.id)
            .pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: dto => {
                    this.dataSource.data = dto.data
                }
            })
    }

    save() {
        this.progress = true
        var obsv = this.measureTypeInput.id ? this.api.updateMeasureType(this.measureTypeInput)
            : this.api.addMeasureType(this.measureTypeInput)
        obsv.pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.measureTypes = result.measureTypes
                    let current = result.measureTypes.find(t => t.id === result.id)
                    this.selectedMeasureType = current ?? result.measureTypes[0]
                    this.loadTable()
                }
            })
    }

    doFilter() {
        this.drawer = { title: "Filter", position: "start", filter: true }
    }

    doAddType() {
        this.drawer = { title: "Add Measure Type", position: "end", filter: false }
        this.measureTypeInput = { id: 0, name: "", description: "" }
    }

    doEditType() {
        this.drawer = { title: "Edit Measure Type", position: "end", filter: false }
        this.measureTypeInput = { ...this.selectedMeasureType }
    }

    @HostListener("window:keyup", ["$event"])
    keyEvent(event: KeyboardEvent) {
        let input: any = document.querySelector("mat-form-field input")
        switch (event.code) {
            case "Slash":
                input.focus()
                break
            case "Escape":
                input.blur()
                break
        }
    }

    applyFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
    }

    identity(_: number, item: MeasureDefinition) {
        return item.id
    }

    closeError() {
        this.errorMsg = ""
        this.showError = false
    }
}

class ToggleQuery {
    expanded!: any
    toggle(t: any) {
        this.expanded = t === this.expanded ? null : t
    }

    query(t: any) {
        return this.expanded === t
    }
}
