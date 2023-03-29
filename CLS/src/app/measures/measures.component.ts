import { animate, state, style, transition, trigger } from '@angular/animations';
import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { processError } from '../lib/app-constants';
import { RegionTreeComponent } from '../lib/region-tree/region-tree.component';
import { Hierarchy, RegionFilter } from "../_services/hierarchy.service"
import { LoggerService } from '../_services/logger.service';
import { MeasureType } from "../_services/measure-definition.service"
import { MeasureApiResponse, MeasureFilter,
    MeasureService, MeasureApiParams, RegionActiveCalculatedDto } from "../_services/measure.service"

interface MeasuresTableRow {
    id: number
    measureDefinition: string
    [key: string]: number | string | RegionActiveCalculatedDto
}

@Component({
  selector: 'app-measures',
  templateUrl: './measures.component.html',
  styleUrls: ['./measures.component.scss'],
  animations: [
      trigger("detailExpand", [
          state("false", style({ height: "0px", minHeight: "0" })),
          state("true", style({ height: "*" })),
          transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
      ])]
})
export class MeasuresComponent implements OnInit {
    measureResponse: MeasureApiResponse | undefined;

    //Declarations
    title = "Measures"
    //filters: any = null;
    filters!: MeasureFilter
    filtersSelected: string[] = []
    dataSource = new MatTableDataSource<MeasuresTableRow>()
    displayedColumns = ["measureDefinition"]
    expandDetail = new ToggleQuery()
    @ViewChild(MatSort) sort!: MatSort
    measureTypes: MeasureType[] = []
    selectedMeasureType: MeasureType = { id: 0, name: "" }
    selectedHierarchy: Hierarchy = {
        id: 0, name: "",
        levelId: 0,
        parentId: 0
    }
    disabledAll = false
    errorMsg: any = ""
    showError = false
    showContentPage = false
    editingMeasureType!: MeasureType
    hierarchyId: number | null = null;
    measureTypeId: number | null = null;
    btnDisabled = false;
    skTargets = "";
    allow = false;
    confirmed = false;
    dataConfirmed: any = {
        target: null,
        yellow: null,
        data: null,
        isApplyToChildren: false,
        isCurrentUpdate: false,
        confirmIntervals: null,
        targetId: null,
        targetCount: null
    };

    hierarchy: RegionFilter[] = []
    selectedRegion = null as number | number[] | null
    @ViewChild(RegionTreeComponent) tree!: RegionTreeComponent
    model = {
        id: 0,
        active: false,
        name: "",
        level: 0,
        selectedParent: null as number | number[] | null
    }
    filtered: MeasureApiParams = {
        hierarchyId: 1,
        measureTypeId: 1,
    }

    constructor(private api: MeasureService, public logger: LoggerService ) { }

    ngOnInit(): void {
        this.api.getMeasureFilter().subscribe({
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

        this.api.getMeasures(params).subscribe({
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

    getMeasures(filtered: MeasureApiParams): void {
        this.showError = false;
        this.disabledAll = true;
        //this.data = null;
        console.log("get target on the component");
        // Call Server
        this.api.getMeasures(filtered).subscribe({
            next: dto => {

                // const firstObject = response.data[0];
                // this.displayedDynamicColumns = Object.keys(firstObject);

                // Extract keys from the first object in the response array
                //const keys = Object.keys(response.data[0].hierarchy[0]);
                // Define column definitions for each key
                // const columns = keys.map((key) => ({
                //     key,
                //     header: key.toUpperCase(),
                //     cell: (element: { [x: string]: any; }) => element[key],
                // }));

                // // Extract the key names into the displayedColumns array
                // this.displayedDynamicColumns = columns.map((column) => column.key);

                // Get all unique keys in the hierarchy objects
                // const hierarchyKeys = Array.from(
                //     new Set(response.data.flatMap((element) => Object.keys(element.hierarchy[0])))
                // );

                // // Generate the displayedColumns array with hierarchy keys
                // const displayedDynamicColumns = ["id", "name", "owner", ...hierarchyKeys.map(key => `hierarchy.${key}`)];

                // this.displayedDynamicColumns = displayedDynamicColumns;
                // console.log("displayedDynamicColumns: ", this.displayedDynamicColumns);

                this.measureResponse = dto;
                this.allow = dto.allow
                //this.confirmed = response.confirmed
                this.dataSource.data = dto.data.map(d => ({
                    id: d.id,
                    measureDefinition: d.name,
                    ...Object.fromEntries(d.hierarchy.map((k, i) => [dto.hierarchy[i], k]))
                }))
                this.displayedColumns.splice(1, Infinity, ...dto.hierarchy)

                //-------Test----------//

                // add the 'name' and 'owner' properties to the displayed columns array
                this.displayedColumns.push('name', 'owner');

                // iterate over each object in the array
                dto.data[0].hierarchy.forEach(obj => {

                    this.displayedColumns.push(obj.id.toString());
                });

                this.displayedColumns.push('actions');

                //this.hierarchy = response.hierarchy;
                this.disabledAll = false;
            },
            error: (err: any) => {
                this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
            }
        })
    }



    // getFiltersFromMain(): void {
    //   this.showError = false;
    //   this.disabledAll = true;
    //   this.filters = null;
    //   this.progress(true);
    //   // Call Server
    //   pages.targetsFilter.get(
    //     {},
    //     value => {
    //       if (itgIsNull(value.error)) {
    //         this.filters = value;
    //         this.$broadcast('filtersEvent');
    //         this.progress(false);
    //       } else {
    //         this.processLocalError('Filters', value.error.message, value.error.id, null, value.error.authError);
    //       }
    //       this.disabledAll = false;
    //     },
    //     err => {
    //       this.processLocalError('Filters', err.statusText, null, err.status, null);
    //     }
    //   );
    // }

    edit(data: any) {
        this.skTargets = "edit";
    }

    cancel(data: any) {
        this.skTargets = "cancel";
    }

    processLocalError(name: string, message: string, id: any, status: unknown, authError: any) {
        this.errorMsg = processError(name, message, id, status)
        this.showError = true
        this.disabledAll = false
        this.showContentPage = (authError != true)
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
