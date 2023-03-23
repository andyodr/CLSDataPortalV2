import { animate, state, style, transition, trigger } from '@angular/animations';
import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { processError } from '../lib/app-constants';
import { Data, MeasureApiParams, MeasureApiResponse, MeasureFilter, MeasureType } from '../_models/measure';
import { Hierarchy, RegionFilter } from '../_models/regionhierarchy';
import { LoggerService } from '../_services/logger.service';
import { MeasureService } from '../_services/measure.service';

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
    @Output() progressEvent = new EventEmitter<boolean>();

    //Declarations
    title = "Measures"
    //filters: any = null;
    filters!: MeasureFilter
    filtersSelected: string[] = []
    dataSource = new MatTableDataSource([] as Data[])
    //dataSource = new MatTableDataSource<Measure>()
    //dataSource = new MatTableDataSource<Data>()

    displayedColumns = ["name", "id", "owner", "hierarchy", "actions"]
    displayedDynamicColumns : string [] = []

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
    drawer = {
        title: "Filter",
        button: "Apply",
        position: "start" as "start" | "end"
    }

    editingMeasureType!: MeasureType

    data: Data [] = [];
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

    private userSubscription = new Subscription()
    hierarchy: RegionFilter[] = []
    hierarchyLevels!: { name: string, id: number }[]
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

    constructor(private measureService: MeasureService, public logger: LoggerService ) { }

    ngOnInit(): void {

        //console.log("target init");
        //this.targetService.getTarget()
        //this.getTargets(this.filtered)
        this.measureService.getMeasureFilter().subscribe({
            next: filters => {
                this.filters = filters;
                console.log("filters on component: ", this.filters);

                this.dataSource.sort = this.sort;
                //this.dataSource = filters.data;
                console.log("filters datasort sort: ", this.dataSource.sort);
                
                this.measureTypes = filters.measureTypes;
                console.log("filters measureTypes: ", this.measureTypes);
                this.selectedMeasureType = filters.measureTypes[0];
                console.log("filters selected measure Types: ", this.selectedMeasureType);

                this.hierarchy = filters.hierarchy;
                console.log("filters hierarchy: ", this.hierarchy);
                //this.selectedHierarchy = filters.hierarchy[0];
                console.log("filters selected hierarchy: ", this.selectedHierarchy);


                //this.loadTable()
                this.getMeasures(this.filtered);
            }
        })
    }

    loadTable() {
        this.filtersSelected = [this.selectedMeasureType.name]
        this.measureService.getMeasure2(this.filtered).subscribe({
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

    save() {
        this.loadTable()
    }

    applyFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
    }

    identity(_: number, item: Data) {
        return item.id
    }
    // identity(_: number, item: { id: number }) {
    //     return item.id
    // }

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
        this.measureService.getMeasure2(filtered).subscribe({
            next: response => {
                
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




                //-----------------//
                this.measureResponse = response;
                this.data = response.data
                console.log("Measures on Component: ", response);
                //this.dataSource = new MatTableDataSource(response.data)
                //console.log("Datasource: ", this.dataSource);
                //this.dataSource.sort = this.sort
                this.allow = response.allow
                //this.confirmed = response.confirmed
                this.dataSource = new MatTableDataSource(response.data)
                
                //-------Test----------//

                // add the 'name' and 'owner' properties to the displayed columns array
                this.displayedDynamicColumns.push('name', 'owner');

                // iterate over each object in the array
                response.data[0].hierarchy.forEach(obj => {
                    
                    this.displayedDynamicColumns.push(obj.id.toString());
                });

                this.displayedDynamicColumns.push('actions');

                console.log("displayedDynamicColumns: ", this.displayedDynamicColumns);




                //-----------------//




                //this.hierarchy = response.hierarchy;
                this.dataSource.sort = this.sort
                console.log("Datasource: ", this.dataSource)
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