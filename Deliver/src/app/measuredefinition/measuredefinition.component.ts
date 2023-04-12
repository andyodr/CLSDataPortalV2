import { animate, state, style, transition, trigger } from "@angular/animations"
import { Component, OnInit, ViewChild } from "@angular/core"
import { MatSort } from "@angular/material/sort"
import { MatTableDataSource } from "@angular/material/table"
import { LoggerService } from "../_services/logger.service"
import { MeasureDefinition, FilterResponseDto, MeasureDefinitionService, MeasureType } from "../_services/measure-definition.service"
import { finalize } from "rxjs"

@Component({
    selector: "app-measuredefinition",
    templateUrl: "./measuredefinition.component.html",
    styleUrls: ["./measuredefinition.component.css"],
    animations: [
        trigger("detailExpand", [
            state("false", style({ height: "0px", minHeight: "0" })),
            state("true", style({ height: "*" })),
            transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
        ])]
})
export class MeasureDefinitionComponent implements OnInit {
    title = "Measure Definition"
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

    constructor(private api: MeasureDefinitionService, public logger: LoggerService) { }

    ngOnInit(): void {
        this.progress = true
        this.api.getMeasureDefinitionFilter()
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: filters => {
                    this.filters = filters
                    this.measureTypes = filters.measureTypes
                    this.selectedMeasureType = filters.measureTypes[0]
                    this.dataSource.sort = this.sort
                    this.loadTable()
                }
            })
    }

    loadTable() {
        this.filtersSelected = [this.selectedMeasureType.name]
        this.progress = true
        this.api.getMeasureDefinition(this.selectedMeasureType.id)
            .pipe(finalize(() => this.progress = false))
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
        obsv.pipe(finalize(() => this.progress = false))
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

    /** filter input keyup event */
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
