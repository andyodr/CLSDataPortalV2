import { Component, OnInit, ViewChild } from "@angular/core"
import { MatSort } from "@angular/material/sort"
import { MatTableDataSource } from "@angular/material/table"
import { LoggerService } from "../_services/logger.service"
import { MeasureDefinition, MeasureDefinitionFilter, MeasureDefinitionService, MeasureType } from "../_services/measure-definition.service"

@Component({
    selector: "app-measuredefinition",
    templateUrl: "./measuredefinition.component.html",
    styleUrls: ["./measuredefinition.component.css"]
})
export class MeasureDefinitionComponent implements OnInit {
    title = "Measure Definition"
    filters!: MeasureDefinitionFilter
    filtersSelected: string[] = []
    dataSource = new MatTableDataSource<MeasureDefinition>()
    displayedColumns = ["name", "varName", "description", "expression", "calculated", "interval", "priority"]
    @ViewChild(MatSort) sort!: MatSort
    measureTypes: MeasureType[] = []
    selectedMeasureType!: MeasureType
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = false
    constructor(private api: MeasureDefinitionService, public logger: LoggerService) { }

    ngOnInit(): void {
        this.api.getMeasureDefinitionFilter().subscribe({
            next: filters => {
                this.filters = filters
                this.measureTypes = filters.measureTypes
                this.dataSource.sort = this.sort
                this.loadTable()
            }
        })
    }

    loadTable() {
        this.selectedMeasureType ??= this.measureTypes[0]
        this.filtersSelected = [this.selectedMeasureType.name]
        this.api.getMeasureDefinition(this.selectedMeasureType.id).subscribe({
            next: dto => {
                this.dataSource.data = dto.data
            }
        })
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
