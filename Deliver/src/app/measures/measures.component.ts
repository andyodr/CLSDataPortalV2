import { animate, state, style, transition, trigger } from "@angular/animations"
import { Component, OnInit, ViewChild } from "@angular/core"
import { MatSort } from "@angular/material/sort"
import { MatTable, MatTableDataSource } from "@angular/material/table"
import { processError } from "../lib/app-constants"
import { RegionTreeComponent } from "../lib/region-tree/region-tree.component"
import { RegionFilter } from "../_services/hierarchy.service"
import { LoggerService } from "../_services/logger.service"
import { MeasureType } from "../_services/measure-definition.service"
import {
    MeasureApiResponse, MeasureFilter,
    MeasureService, RegionActiveCalculatedDto
} from "../_services/measure.service"
import { finalize } from "rxjs"

interface MeasuresTableRow {
    id: number
    measureDefinition: string
    [key: string]: number | string | RegionActiveCalculatedDto
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

@Component({
    selector: "app-measures",
    templateUrl: "./measures.component.html",
    styleUrls: ["./measures.component.scss"],
    animations: [
        trigger("detailExpand", [
            state("false", style({ height: "0px", minHeight: "0" })),
            state("true", style({ height: "*" })),
            transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
        ])]
})
export class MeasuresComponent implements OnInit {
    title = "Measures"
    measureResponse: MeasureApiResponse | undefined;
    filters!: MeasureFilter
    filtersSelected: string[] = []
    dataSource = new MatTableDataSource<MeasuresTableRow>()
    displayedColumns = ["measureDefinition"]
    expand = new ToggleQuery()
    @ViewChild(MatTable) matTable!: MatTable<MeasuresTableRow>
    @ViewChild(MatSort) sort!: MatSort
    measureTypes: MeasureType[] = []
    selectedMeasureType: MeasureType = { id: 0, name: "" }
    progress = false
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = false
    editingMeasureType!: MeasureType
    hierarchyId: number | null = null;
    measureTypeId: number | null = null;
    btnDisabled = false;
    allow = false;
    hierarchy: RegionFilter[] = []
    selectedRegion = null as number | number[] | null
    @ViewChild(RegionTreeComponent) tree!: RegionTreeComponent

    constructor(private api: MeasureService, public logger: LoggerService) { }

    ngOnInit(): void {
        this.progress = true
        this.api.getMeasureFilter()
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: dto => {
                    this.filters = dto
                    this.measureTypes = dto.measureTypes
                    this.selectedMeasureType = dto.measureTypes[0]
                    this.hierarchy = dto.hierarchy
                    this.selectedRegion = dto.filter.hierarchyId ?? dto.hierarchy.at(0)?.id ?? 1
                    // delay because it depends on output from a child component
                    setTimeout(() => this.filtersSelected = [this.selectedMeasureType.name, this.tree.ancestorPath.join(" | ")])
                    this.dataSource.sort = this.sort
                    this.dataSource.data = dto.measures.data.map(d => ({
                        id: d.id,
                        measureDefinition: d.name,
                        ...Object.fromEntries(d.hierarchy.map((r, i) => [dto.measures.hierarchy[i], r]))
                    }))
                    this.displayedColumns.splice(1, Infinity, ...dto.measures.hierarchy)
                }
            })
    }

    loadTable() {
        this.filtersSelected = [this.selectedMeasureType.name, this.tree.ancestorPath.join(" | ")]
        const params = {
            measureTypeId: this.selectedMeasureType.id,
            hierarchyId: typeof this.selectedRegion === "number" ? this.selectedRegion : (this.hierarchy.at(0)?.id ?? 1)
        }

        this.progress = true
        this.api.getMeasures(params)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: dto => {
                    this.dataSource.data = dto.data.map(d => ({
                        id: d.id,
                        measureDefinition: d.name,
                        ...Object.fromEntries(d.hierarchy.map((r, i) => [dto.hierarchy[i], r]))
                    }))
                    this.displayedColumns.splice(1, Infinity, ...dto.hierarchy)
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

    closeError() {
        this.errorMsg = ""
        this.showError = false
    }

    save(row: MeasuresTableRow) {
        const dto = {
            measureDefinitionId: row.id,
            hierarchy: Object.values(row)
                .filter((v): v is RegionActiveCalculatedDto => typeof v === "object")
        }

        this.progress = true
        this.api.updateMeasures(dto)
            .pipe(finalize(() => this.progress = false))
            .subscribe(response => {
                let i = this.dataSource.data.findIndex(d => d.id === response.data.at(0)?.id)
                if (i >= 0) {
                    this.dataSource.data = [...this.dataSource.data.slice(0, i), response.data.map(d => ({
                        id: d.id,
                        measureDefinition: d.name,
                        ...Object.fromEntries(d.hierarchy.map((r, i) => [d.hierarchy[i], r]))
                    }))[0], ...this.dataSource.data.slice(i + 1)]
                    this.loadTable()
                }
                else {
                    this.logger.logWarning(`Edited measureDefinitionId=${ row.id } missing from response`)
                }
            })
    }

    processLocalError(name: string, message: string, id: any, status: unknown, authError: any) {
        this.errorMsg = processError(name, message, id, status)
        this.showError = true
        this.disabledAll = false
        this.showContentPage = (authError != true)
    }
}
