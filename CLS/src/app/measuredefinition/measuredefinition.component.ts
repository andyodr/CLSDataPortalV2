import { animate, state, style, transition, trigger } from "@angular/animations"
import { Component, OnInit, ViewChild } from "@angular/core"
import { MatSort } from "@angular/material/sort"
import { MatTableDataSource } from "@angular/material/table"
import { LoggerService } from "../_services/logger.service"
import { MeasureDefinition, FilterResponseDto, MeasureDefinitionService, MeasureType } from "../_services/measure-definition.service"

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
    displayedColumns = ["name", "varName", "description", "calculated", "interval", "priority"]
    expandDetail = new ToggleQuery()
    @ViewChild(MatSort) sort!: MatSort
    measureTypes: MeasureType[] = []
    selectedMeasureType: MeasureType = { id: 0, name: "" }
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = false
    drawer = {
        title: "Filter",
        button: "Apply",
        position: "start" as "start" | "end"
    }

    editingMeasureType!: MeasureType

    constructor(private api: MeasureDefinitionService, public logger: LoggerService) { }

    ngOnInit(): void {
        this.api.getMeasureDefinitionFilter().subscribe({
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
        this.api.getMeasureDefinition(this.selectedMeasureType.id).subscribe({
            next: dto => {
                this.dataSource.data = dto.data
            }
        })
    }

    doFilter() {
        this.drawer = { title: "Filter", button: "Apply", position: "start" }
    }

    doAddType() {
        this.drawer = { title: "Add Measure Type", button: "Save", position: "end" }
        this.editingMeasureType = { id: 0, name: "", description: "" }
    }

    doEditType() {
        this.drawer = { title: "Edit Measure Type", button: "Save", position: "end" }
        this.editingMeasureType = { ...this.selectedMeasureType }
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
