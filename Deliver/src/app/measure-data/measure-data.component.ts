import { Component, DestroyRef, ViewChild, inject } from '@angular/core';
import { Intervals } from "../lib/app-constants"
import { MeasureDataDto, MeasureDataApiResponse, MeasureDataFilterResponseDto, FiltersIntervalsData } from '../_models/measureData';
import { MeasureDataService } from "../_services/measure-data.service"
import { MatTableDataSource, MatTableModule } from "@angular/material/table"
import { MatSort, MatSortModule } from "@angular/material/sort"
import { finalize } from "rxjs"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { HttpParams } from '@angular/common/http';
import { LoggerService } from '../_services/logger.service';
import { RegionFilter } from '../_services/hierarchy.service';
import { IntervalDto, MeasureType } from '../_services/measure-definition.service';
import { RegionTreeComponent } from '../lib/region-tree/region-tree.component';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { MatDialog } from '@angular/material/dialog';
import { AppDialog } from '../app-dialog.component';
import { AccountService } from '../_services/account.service';
import { NgClass, DatePipe, DecimalPipe, PercentPipe } from "@angular/common"
import { MatTooltipModule } from "@angular/material/tooltip"
import { MatInputModule } from "@angular/material/input"
import { NgbAlert } from "@ng-bootstrap/ng-bootstrap"
import { ErrorsComponent } from "../errors/errors.component"
import { SidebarComponent } from "../nav/sidebar.component"
import { MatOptionModule } from "@angular/material/core"
import { MatSelectModule } from "@angular/material/select"
import { MatFormFieldModule } from "@angular/material/form-field"
import { FormsModule } from "@angular/forms"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatSidenavModule } from "@angular/material/sidenav"
import { MatProgressBarModule } from "@angular/material/progress-bar"

@Component({
    selector: 'app-measure-data',
    templateUrl: './measure-data.component.html',
    styleUrls: ['./measure-data.component.scss'],
    animations: [
        trigger("detailExpand", [
            state("false", style({ height: "0px", minHeight: "0" })),
            state("true", style({ height: "*" })),
            transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
        ])
    ],
    standalone: true,
    imports: [DecimalPipe, PercentPipe, MatProgressBarModule, MatSidenavModule, MatButtonModule, MatIconModule,
        FormsModule, MatFormFieldModule, MatSelectModule, MatOptionModule, RegionTreeComponent, SidebarComponent,
        ErrorsComponent, NgbAlert, MatInputModule, MatTooltipModule, MatTableModule, MatSortModule, NgClass, DatePipe]
})
export class MeasureDataComponent {

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
    selectedRow: MeasureDataDto | undefined
    dataSource = new MatTableDataSource<MeasureDataDto>()
    displayedColumns = ["name", "calculated", "value", "units", "explanation", "action", "updated"]
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
    destroyRef = inject(DestroyRef)

    constructor(private measureDataSvc: MeasureDataService, private acctSvc: AccountService, private logger: LoggerService, private dialog: MatDialog) {
        this.progress = true
        this.measureDataSvc.getFilters()
            .pipe(takeUntilDestroyed(), finalize(() => this.progress = false))
            .subscribe({
                next: filter => {
                    this.filters = filter
                    this.select = {
                        intervals: filter.intervals ?? [],
                        years: filter.years ?? [],
                        weeks: [],
                        months: [],
                        quarters: [],
                        measureTypes: filter.measureTypes,
                        hierarchy: filter.hierarchy ?? []
                    }

                    let { intervalId, measureTypeId, hierarchyId } = filter.filter
                    const saved = this.acctSvc.getCurrentUser()?.filter
                    measureTypeId = saved?.measureTypeId || measureTypeId
                    hierarchyId = saved?.hierarchyId || hierarchyId
                    this.model.fMeasureTypeSelected = filter.measureTypes.find(m => m.id === (saved?.measureTypeId ?? measureTypeId))
                    this.model.selectedRegion = hierarchyId ?? this.select.hierarchy.at(0)?.id
                    this.model.fIntervalSelected = filter.intervals?.find(n => n.id === (saved?.intervalId ?? intervalId))
                    this.model.fYearSelected = filter.years?.find(n => n.year == (saved?.year ?? new Date().getFullYear()))
                    this.intervalChange(true)
                }
            })
    }

    ngAfterViewInit() {
        this.dataSource.sort = this.sort;
        this.dataSource.sortingDataAccessor = (item, property) => {
            switch (property) {
                case 'name': return item.name;
                case 'calculated': return item.calculated;
                case 'value': return item.value;
                case 'units': return item.units;
                case 'explanation': return item.explanation;
                case 'action': return item.action;
                case 'updated': return item.updated.longDt;
                default: return (item as any)[property];
            }
        };
    }

    // -----------------------------------------------------------------------------
    // Filter Selection
    // -----------------------------------------------------------------------------
    doFilter() {
        this.drawer = { title: "Filter", button: "Apply", position: "start" }
    }

    /** Initialize Week/Month/Quarter select menus in Filter drawer after Interval or Year changes **/
    intervalChange(ngOnInit = false) {
        const { fIntervalSelected, fYearSelected } = this.model
        if (fYearSelected == null || fIntervalSelected == null) return
        let params = new HttpParams()
            .set("intervalId", fIntervalSelected.id)
            .set("year", (fYearSelected.year).toString())
        this.measureDataSvc.getFiltersIntervals(params)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: dto => {
                let { intervalId, calendarId } = this.filters.filter
                if (intervalId != fIntervalSelected.id || !calendarId) {
                    calendarId = dto.calendarId
                }

                const saved = this.acctSvc.getCurrentUser()?.filter
                switch (fIntervalSelected.id) {
                    case Intervals.Weekly:
                        this.select.weeks = dto.data
                        const { weeks } = this.select
                        this.model.fWeekSelected = weeks.find(w => w.number === saved?.week) ??
                            weeks.find(w => w.id === calendarId)
                        break
                    case Intervals.Monthly:
                        this.select.months = dto.data
                        const { months } = this.select
                        this.model.fMonthSelected = months.find(m => m.month === saved?.month) ??
                            months.find(m => m.id === calendarId)
                        break
                    case Intervals.Quarterly:
                        this.select.quarters = dto.data
                        const { quarters } = this.select
                        //this.model.fQuarterSelected = quarters.find(w => w.id === calendarId)
                        this.model.fQuarterSelected = quarters.find(q => q.number === saved?.quarter) ??
                            quarters.find(q => q.id === calendarId)
                        break
                }

                this.select.measureTypes = dto.measureTypes
                this.model.fMeasureTypeSelected = dto.measureTypes.find(m => m.id === (saved?.measureTypeId))
            if (ngOnInit) {
                    this.loadTable(false)
                }
            }
        })
    }

    // -----------------------------------------------------------------------------
    // Load Table Data
    // -----------------------------------------------------------------------------
    loadTable(saveFilters = true): void {
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
            default:
                return
        }

        if (!fMeasureTypeSelected) return
        params.measureTypeId = fMeasureTypeSelected.id
        if (!this.model.selectedRegion || Array.isArray(this.model.selectedRegion)) return
        if (saveFilters) {
            this.acctSvc.saveSettings({
                intervalId: fIntervalSelected.id,
                measureTypeId: fMeasureTypeSelected.id,
                hierarchyId: this.model.selectedRegion,
                ...(this.model.fYearSelected && { year: this.model.fYearSelected.year }),
                ...(this.model.fWeekSelected && { week: this.model.fWeekSelected.number }),
                ...(this.model.fMonthSelected && { month: this.model.fMonthSelected.month }),
                ...(this.model.fQuarterSelected && { quarter: this.model.fQuarterSelected.number })
            })
        }

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
            .pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: measureDataResponse => {
                    this.measureDataResponse = measureDataResponse
                    this.measureDataList = measureDataResponse.data
                    this.dataSource.data = measureDataResponse.data
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
        if (this.filterSelected) this.loadTable(false)
    }

    onEdit(measureDataRow: MeasureDataDto) {
        this.isEditMode = true
        this.selectedRow = measureDataRow
        if (!measureDataRow.calculated) {
            this.model.value = measureDataRow.value
        }

        if (measureDataRow.explanation) {
            this.model.explanation = measureDataRow.explanation
        }

        if (measureDataRow.action) {
            this.model.action = measureDataRow.action
        }
    }

    onSave(measureDataRow: MeasureDataDto) {
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
            .pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: measureDataResponse => {
                    this.logger.logInfo("Measure Data Updated")
                    console.log("measureData on updateMeasureData", measureDataResponse);
                    this.disabledAll = false;
                    this.loadTable(false)
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
    }

    onCancel() {
        this.isEditMode = false
        this.disabledAll = false
        this.model.explanation = ""
        this.model.action = ""
        this.model.value = undefined
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

    identity(index: number, item: any) {
        return item.id
    }

    itgIsNull(value: any): boolean {
        return value === undefined || value === null || value !== value;
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
