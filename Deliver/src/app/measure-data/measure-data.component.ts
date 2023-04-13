import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { Intervals, MSG_ERROR_PROCESSING } from '../lib/app-constants';
import { MeasureDataDto, MeasureDataApiResponse, MeasureDataFilterResponseDto, FiltersIntervalsData } from '../_models/measureData';
import { MeasureDataService } from "../_services/measure-data.service"
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { filter, finalize, Subscription } from 'rxjs';
import { NavigationService } from '../_services/nav.service';
import { HttpClient, HttpParams } from '@angular/common/http';
import { LoggerService } from '../_services/logger.service';
import { Hierarchy, RegionFilter, RegionFlatNode } from '../_services/hierarchy.service';
import { IntervalDto, MeasureType } from '../_services/measure-definition.service';
import { RegionTreeComponent } from '../lib/region-tree/region-tree.component';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { MatDialog } from '@angular/material/dialog';
import { AppDialog } from '../app-dialog.component';
import { AccountService } from '../_services/account.service';


@Component({
    selector: 'app-measure-data',
    templateUrl: './measure-data.component.html',
    styleUrls: ['./measure-data.component.scss'],
    animations: [
        trigger("detailExpand", [
            state("false", style({ height: "0px", minHeight: "0" })),
            state("true", style({ height: "*" })),
            transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
        ])]
})
export class MeasureDataComponent implements OnInit {

    title = 'Measure Data';
    measureDataResponse: MeasureDataApiResponse | undefined;

    //Filter Properties
    drawer = {
        title: "Filter",
        button: "Apply",
        position: "start" as "start" | "end"
    }
    filters!: MeasureDataFilterResponseDto
    filterSelected: string[] = []
    select = {
        intervals: [] as IntervalDto[],
        years: [] as { id: number, year: number }[],
        weeks: [] as FiltersIntervalsData[],
        months: [] as FiltersIntervalsData[],
        quarters: [] as FiltersIntervalsData[],
        measureTypes: [] as MeasureType[],
        hierarchy: [] as RegionFilter[]
    }
    hierarchy: RegionFilter[] = []
    hierarchyLevels!: { id: number, name: string }[]
    @ViewChild(RegionTreeComponent) treeControl!: RegionTreeComponent
    intervalList!: IntervalDto[]
    yearList!: { name: string, id: number }[]
    measureTypeList!: { name: string, id: number }[]
    Intervals = Intervals

    // Selection Calculated
    selCalculated = [
        { id: 0, name: "Manual and Calculated" },
        { id: 1, name: "Manual" },
        { id: 2, name: "Calculated" }
    ];

    //Table Properties
    measureDataList: MeasureDataDto[] = [];
    //measureDataRow: MeasureDataDto | undefined
    selectedRow: MeasureDataDto | undefined
    dataSource = new MatTableDataSource<MeasureDataDto>()
    displayedColumns = ["name", "calculated", "value", "units", "explanation", "action", "updated", "rowactions"]
    @ViewChild(MatSort) sort!: MatSort
    selectedMeasureType: MeasureType = { id: 0, name: "" }
    isEditMode = false
    expandDetail = new ToggleQuery()

    //Local Properties
    progress = false
    showContentPage = true
    dataRange = "";
    disabledAll = true;
    btnDisabled = false;
    skMeasureData = "";
    allow = false;
    editValue = false;
    showActionButtons = true;
    //editBgColor = true;
    //filteredPage = null;
    locked: boolean | undefined
    calendarId!: number;
    day!: string;
    hierarchyId!: number;
    measureTypeId!: number;

    //Model
    model = {
        fIntervalSelected: undefined as IntervalDto | undefined,
        fYearSelected: undefined as { id: number, year: number } | undefined,
        fWeekSelected: undefined as FiltersIntervalsData | undefined,
        fMonthSelected: undefined as FiltersIntervalsData | undefined,
        fQuarterSelected: undefined as FiltersIntervalsData | undefined,
        fMeasureTypeSelected: undefined as MeasureType | undefined,
        id: 0,
        active: false,
        name: "",
        interval: 0,
        year: 0,
        quarter: 0,
        measureType: 0,
        selectedRegion: null as number | number[] | null,
        explanation: "",
        action: "",
        value: undefined as number | undefined,
        selCalSelected: 0,
    }

    //Error handling within the component
    errorMsg: any = ""
    showError: boolean = false;

    constructor(private measureDataSvc: MeasureDataService, private acctSvc: AccountService, private logger: LoggerService, private dialog: MatDialog) { }

    ngOnInit(): void {
        this.progress = true
        this.measureDataSvc.getFilters()
        .pipe(finalize(() => this.progress = false))
        .subscribe({
            next: dtoFilter => {
                this.filters = dtoFilter
                this.select = {
                    intervals: dtoFilter.intervals ?? [],
                    years: dtoFilter.years ?? [],
                    weeks: [],
                    months: [],
                    quarters: [],
                    measureTypes: dtoFilter.measureTypes,
                    hierarchy: dtoFilter.hierarchy ?? []
                }

                let { intervalId, measureTypeId, hierarchyId } = dtoFilter.filter
                measureTypeId = this.acctSvc.getCurrentUser()?.filter.measureTypeId ?? measureTypeId
                this.model.fMeasureTypeSelected = measureTypeId ? dtoFilter.measureTypes.find(m => m.id === measureTypeId) : dtoFilter.measureTypes.at(0)
                this.model.selectedRegion = hierarchyId ?? this.select.hierarchy[0].id
                this.model.fIntervalSelected = dtoFilter.intervals?.find(n => n.id === intervalId)
                this.model.fYearSelected = dtoFilter.years?.at(0)
                this.intervalChange(true)
            }
        })
    }

    ngAfterViewInit() {
        this.dataSource.sort = this.sort;
    }

    // -----------------------------------------------------------------------------
    // Filter Selection
    // -----------------------------------------------------------------------------
    doFilter() {
        this.drawer = { title: "Filter", button: "Apply", position: "start" }
    }

    /** Initialize Week/Month/Quarter select menus in Filter drawer after Interval or Year changes **/
    intervalChange(loadTable = false) {
        const { fIntervalSelected, fYearSelected } = this.model
        if (fIntervalSelected?.id == Intervals.Yearly || fYearSelected == null || fIntervalSelected == null) return
        let params = new HttpParams()
            .set("intervalId", fIntervalSelected.id)
            .set("year", (fYearSelected.year).toString())
        this.measureDataSvc.getFiltersIntervals(params).subscribe({
            next: dto => {
                let { intervalId, calendarId } = this.filters.filter
                if (intervalId != fIntervalSelected.id || !calendarId) {
                    calendarId = dto.calendarId
                }
                switch (fIntervalSelected.id) {
                    case Intervals.Weekly:
                        this.select.weeks = dto.data
                        const { weeks } = this.select
                        this.model.fWeekSelected = weeks.find(w => w.id === calendarId)
                        break
                    case Intervals.Monthly:
                        this.select.months = dto.data
                        const { months } = this.select
                        this.model.fMonthSelected = months.find(w => w.id === calendarId)
                        break
                    case Intervals.Quarterly:
                        this.select.quarters = dto.data
                        const { quarters } = this.select
                        this.model.fQuarterSelected = quarters.find(w => w.id === calendarId)
                        break
                }
                if (loadTable) {
                    this.loadTable()
                }
            }
        })
    }

    // -----------------------------------------------------------------------------
    // Load Table Data
    // -----------------------------------------------------------------------------
    loadTable(): void {
        const { fMeasureTypeSelected, fIntervalSelected, fWeekSelected, fMonthSelected, fQuarterSelected, fYearSelected } = this.model
        this.filterSelected[0] = fIntervalSelected?.name ?? "?"
        this.filterSelected[1] = fMeasureTypeSelected?.name ?? "?"
        this.filterSelected[2] = this.treeControl?.ancestorPath?.join(" | ") ?? "?"

        const params = { calendarId: 0, measureTypeId: 0, hierarchyId: 0 }
        switch (fIntervalSelected?.id) {
            case Intervals.Weekly:
                if (!fWeekSelected) return
                params.calendarId = fWeekSelected.id
                break
            case Intervals.Monthly:
                if (!fMonthSelected) return
                params.calendarId = fMonthSelected.id
                break
            case Intervals.Quarterly:
                if (!fQuarterSelected) return
                params.calendarId = fQuarterSelected.id
                break
            case Intervals.Yearly:
                if (!fYearSelected) return
                params.calendarId = fYearSelected.id
                break
        }
        if (!fMeasureTypeSelected) return
        this.acctSvc.saveFilter({ measureTypeId: fMeasureTypeSelected.id })
        params.measureTypeId = fMeasureTypeSelected.id
        if (!this.model.selectedRegion || Array.isArray(this.model.selectedRegion)) return
        params.hierarchyId = this.model.selectedRegion

        this.getMeasureDataList(params)
    }

    getMeasureDataList(parameters: { calendarId: any; measureTypeId: any; hierarchyId: any; } | undefined) {
        this.showError = false;
        this.disabledAll = true;
        this.dataRange = "";

        this.calendarId = parameters!.calendarId;
        this.hierarchyId = parameters!.hierarchyId;
        this.measureTypeId = parameters!.measureTypeId;

        let params = new HttpParams()
            .set("calendarId", (parameters!.calendarId).toString())
            .set("hierarchyId", (parameters!.hierarchyId).toString())
            .set("measureTypeId", (parameters!.measureTypeId).toString())

        this.progress = true;

        // Call Server - GET Measure Data List
        this.measureDataSvc.getMeasureDataList(params)
        .pipe(finalize(() => this.progress = false))
        .subscribe({
            next: measureDataResponse => {
                this.measureDataResponse = measureDataResponse
                this.measureDataList = measureDataResponse.data
                this.dataSource.data = measureDataResponse.data
                // if (this.dataSource) {
                //     this.dataSource.sort = this.sort;
                //   }
                this.dataSource.sort = this.sort
                console.log("MeasureDataResponse on getMeasureDataList: ", this.measureDataResponse)
                console.log("Datasource on getMeasureDataList: ", this.dataSource)

                this.calendarId = measureDataResponse.calendarId;
                this.hierarchyId = parameters?.hierarchyId;
                this.measureTypeId = parameters?.measureTypeId;

                this.dataRange = measureDataResponse.range;
                this.allow = measureDataResponse.allow;
                this.locked = measureDataResponse.locked;
                this.editValue = measureDataResponse.editValue;
                this.showActionButtons = this.allow && !this.locked;
                this.disabledAll = false;
                this.logger.logInfo("Measure Data List Loaded")
            },
            error: err => {
                this.logger.logError(err.message)
                this.errorMsg = err
                this.showError = true
                this.processLocalError(this.title, err.statusText, null, err.status, null);
            }
        })
    }

    applyTableFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
    }

    // -----------------------------------------------------------------------------
    // Selection Calculated
    // -----------------------------------------------------------------------------
    onSelCalChange(): void {
        console.log("onSelCalChange: ", this.model.selCalSelected);

        if (this.model.selCalSelected == 2) {
            this.dataSource.data = this.measureDataList.filter(item => item.calculated);
        }
        if (this.model.selCalSelected == 1) {
            this.dataSource.data = this.measureDataList.filter(item => !item.calculated);
        }
        if (this.model.selCalSelected == 0) {
            this.dataSource.data = this.measureDataList
        }
        this.dataSource._updateChangeSubscription();
        console.log("Datasource on onSelCalChange: ", this.dataSource.data)
    }

    // -----------------------------------------------------------------------------
    // Buttons
    // -----------------------------------------------------------------------------
    refresh() {
        if (this.filterSelected) this.loadTable();
    }

    onEdit(measureDataRow: MeasureDataDto) {

        //if (!this.allow || this.locked) return;
        this.isEditMode = true;
        this.selectedRow = measureDataRow;
        // this.selectedRow = { ...measureDataRow };
        // this.model.explanation = measureDataRow.explanation;
        // this.model.action = measureDataRow.action;
    }

    onSave(measureDataRow: MeasureDataDto) {

        //if (!this.allow || this.locked) return;

        this.isEditMode = false
        this.selectedRow = { ...measureDataRow };

        this.showError = false;
        this.disabledAll = true;
        //console.log("onSave MeasureDataRow: ", measureDataRow);

        const measureDataId = measureDataRow.id;
        let measureDataValue = measureDataRow.value;
        if (!measureDataRow.calculated) {
            measureDataValue = this.model.value as number;
        }
        const measureDataExplanation = this.model.explanation;
        const measureDataAction = this.model.action;

        const body = {
            "calendarId": this.calendarId,
            "day": this.day,
            "hierarchyId": this.hierarchyId,
            "measureTypeId": this.measureTypeId,
            "measureDataId": measureDataId,
            "measureValue": measureDataValue,
            "explanation": measureDataExplanation,
            "action": measureDataAction
           }

        if (measureDataRow.value == body.measureValue && measureDataRow.explanation == body.explanation && measureDataRow.action == body.action) {
            this.logger.logInfo("There are no changes for " + measureDataRow.name)
            const dialogRef = this.dialog.open(AppDialog, {
                width: '450px',
                data: {
                    title: 'Alert',
                    msg: 'There are no changes for ' + measureDataRow.name
                    //msg: 'There are no changes for ' + measureDataRow.name + '. Unable to Save.'
                }
            });
        }

        this.progress = true;

        // Call Server - PUT Measure Data
        this.measureDataSvc.updateMeasureData(body)
        .pipe(finalize(() => this.progress = false))
        .subscribe({
            next: measureDataResponse => {
                this.logger.logInfo("Measure Data Updated")
                console.log("measureData on updateMeasureData", measureDataResponse);
                this.disabledAll = false;
                this.loadTable();
            },
            error: err => {
                this.logger.logError(err.message)
                this.errorMsg = err
                this.showError = true
                this.processLocalError(this.title, err.statusText, null, err.status, null);
            }
        })
        this.model.explanation = "";
        this.model.action = "";
        this.model.value = undefined;
        //this.loadTable();
    }

    onCancel() {
        this.isEditMode = false;
        this.disabledAll = false;
        this.model.explanation = "";
        this.model.action = "";
        this.model.value = undefined;
        this.loadTable();
    }

    // -----------------------------------------------------------------------------
    // Error Handling
    // -----------------------------------------------------------------------------
    closeError(): void {
        this.errorMsg = "";
        this.showError = false;
    }

    processLocalError(title: string, message: string, id: null | number, status: null | number, authError: boolean | null): void {
        this.errorMsg = this.processError(title, message, id, status);
        this.progress = false;
        this.disabledAll = false;
        this.showContentPage = (authError !== true);
    }

    processError(title: string, message: string, id: number | null, status: number | null): string {
        return title + message  // TODO: finish
    }

    // -----------------------------------------------------------------------------
    // Styles - Not in Use - Handled on Template
    // -----------------------------------------------------------------------------
    getBgColor2(element: MeasureDataDto): string {
        if (!element.value || (!element.target && !element.yellow)) {
            return "";
        }

        if (!element.target) {
            return element.value >= element.yellow ? "bggreen" : "bgred";
        }

        if (!element.yellow) {
            return element.value >= element.target ? "bggreen" : "bgred";
        }

        if (element.target >= element.yellow) {
            if (element.value >= element.yellow) {
                return "bgorange";
            }
            if (element.value >= element.target) {
                return "bggreen";
            }
        }

        if (element.target < element.yellow) {
            if (element.value <= element.yellow) {
                return "bgorange";
            }
            if (element.value <= element.target) {
                return "bggreen";
            }
        }

        return "bgred";
    }

    // -----------------------------------------------------------------------------
    // Utils
    // -----------------------------------------------------------------------------

    identity(index: number, item: any) {
        return item.id
    }

    isBoolShow(str: string | boolean): boolean {
        return ((str === "true") || (str === true));
    }

    itgIsEmpty(value: any): boolean {
        if (!this.itgIsNull(value)) {
            const str = value.toString().trim();
            return str.length === 0;
        }
        return true;
    }

    itgIsNull(value: any): boolean {
        return value === undefined || value === null || value !== value;
    }

    itgStrNullToEmpty(str: string): string {
        let ret = str;
        if (str === null || str === "null") {
            ret = "";
        }
        return ret;
    }

    itgIsNumeric(data: any): boolean {
        return !isNaN(parseFloat(data)) && isFinite(data);
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
