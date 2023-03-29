import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { Intervals, MSG_ERROR_PROCESSING } from '../lib/app-constants';
import { FilterResponseDto, IntervalDto, MeasureDataDto, MeasureDataResponse } from '../_models/measureData';
import { FiltersIntervalsData, MeasureDataService } from "../_services/measure-data.service"
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { filter, Subscription } from 'rxjs';
import { NavigationService } from '../_services/nav.service';
import { HttpClient, HttpParams } from '@angular/common/http';
import { LoggerService } from '../_services/logger.service';
import { Hierarchy, RegionFilter, RegionFlatNode } from '../_models/regionhierarchy';
import { RegionTreeComponent } from '../lib/region-tree/region-tree.component';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { MeasureType } from '../_services/measure-definition.service';

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

    measureDataResponse: MeasureDataResponse | undefined;

    //------------------ Table Properties ------------------
    measureDataList: MeasureDataDto[] = [];
    measureDataRow: MeasureDataDto | undefined
    dataSource = new MatTableDataSource<MeasureDataDto>()
    displayedColumns = ["name", "calculated", "value", "units", "explanation", "action", "updated", "rowactions"]
    @ViewChild(MatSort) sort!: MatSort
    editingMeasureType!: any
    selectedMeasureType: MeasureType = { id: 0, name: "" }

    isEditMode = false
    selectedRow: MeasureDataDto | undefined
    expandDetail = new ToggleQuery()

    //------------------ Local Properties ------------------
    title = 'Measure Data';
    showContentPage = true
    Intervals = Intervals
    dataRange = "";

   
    disabledAll = true;
    btnDisabled = false;
    skMeasureData = "";
    allow = false;
    editValue = false;
    showActionButtons = true;
    editBgColor = true;

    filteredPage = null;

    locked: boolean | undefined

    //------------------ Filter Properties ------------------
    //------------------ Filter Drawer ------------------
    //drawerTitle = "Filter"
    drawer = {
        title: "Filter",
        button: "Apply",
        position: "start" as "start" | "end"
    }
    filters!: FilterResponseDto
    @ViewChild(RegionTreeComponent) treeControl!: RegionTreeComponent
    select = {
        intervals: [] as IntervalDto[],
        years: [] as { id: number, year: number }[],
        weeks: [] as FiltersIntervalsData[],
        months: [] as FiltersIntervalsData[],
        quarters: [] as FiltersIntervalsData[],
        measureTypes: [] as MeasureType[],
        hierarchy: [] as RegionFilter[]
    }
    filterSelected: string[] = []
    // filtered: MeasureDataReceiveObject = {
    //     calendarId: 649,
    //     day: "3/9/2023",
    //     hierarchyId: 1,
    //     measureTypeId: 1,
    //     explanation: 'explanation-value',
    //     action: 'action-value'
    // }

    hierarchy: RegionFilter[] = []
    hierarchyLevels!: { id: number, name: string }[]
    intervalList!: IntervalDto[]
    yearList!: { name: string, id: number }[]
    measureTypeList!: { name: string, id: number }[]

    //------------------ Filter Drawer Model ------------------
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
        selCalSelected: 0,
    }

    //----------------- Error handling within the component
    errorMsg: any = ""
    showError: boolean = false;
    calendarId: any;
    hierarchyId: any;
    measureTypeId: any;

    measureDataListNew: MeasureDataDto[] = [];
    dataSourceCopy: any;
    



    constructor(private measureDataService: MeasureDataService, private logger: LoggerService) { }

    ngOnInit(): void {
        this.measureDataService.getFilters().subscribe({
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

                const { intervalId, measureTypeId, hierarchyId } = dtoFilter.filter
                this.model.fMeasureTypeSelected = measureTypeId ? dtoFilter.measureTypes.find(m => m.id === measureTypeId) : dtoFilter.measureTypes.at(0)
                this.model.selectedRegion = hierarchyId ?? this.select.hierarchy[0].id
                this.model.fIntervalSelected = dtoFilter.intervals?.find(n => n.id === intervalId)
                this.model.fYearSelected = dtoFilter.years?.at(0)
                this.intervalChange(true)
            }
        })
        //this.getData2(this.filtered)
        //this.getMeasureDataList(params)
    }

    /** Initialize Week/Month/Quarter select menus in Filter drawer after Interval or Year changes */
    intervalChange(loadTable = false) {
        const { fIntervalSelected, fYearSelected } = this.model
        if (fIntervalSelected?.id == Intervals.Yearly || fYearSelected == null || fIntervalSelected == null) return
        let params = new HttpParams()
            .set("intervalId", fIntervalSelected.id)
            .set("year", (fYearSelected.year).toString())
        this.measureDataService.getFiltersIntervals(params).subscribe({
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

    loadTable(): void {
        this.filterSelected[0] = this.model.fIntervalSelected?.name ?? "?"
        this.filterSelected[1] = this.model.fMeasureTypeSelected?.name ?? "?"
        this.filterSelected[2] = this.treeControl?.ancestorPath?.join(" | ") ?? "?"

        const { fIntervalSelected, fWeekSelected, fMonthSelected, fQuarterSelected, fYearSelected } = this.model
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

        if (!this.model.fMeasureTypeSelected) return
        params.measureTypeId = this.model.fMeasureTypeSelected.id

        if (!this.model.selectedRegion || Array.isArray(this.model.selectedRegion)) return
        params.hierarchyId = this.model.selectedRegion
        // this.getData(p)
        this.getMeasureDataList(params)
        //this.getData2(this.filtered)
    }

    getMeasureDataList(parameters: { calendarId: any; measureTypeId: any; hierarchyId: any; } | undefined) {
        this.showError = false;
        this.disabledAll = true;
        this.dataRange = "";

        //this.filteredPage = parameters!.calendarId;
        this.calendarId = parameters!.calendarId;
        //this.day = filtered.day;
        this.hierarchyId = parameters!.hierarchyId;
        this.measureTypeId = parameters!.measureTypeId;

        this.progress(true);

        let params = new HttpParams()
            .set("calendarId", (parameters!.calendarId).toString())
            .set("hierarchyId", (parameters!.hierarchyId).toString())
            .set("measureTypeId", (parameters!.measureTypeId).toString())
            console.log(" get measure data list params", params.toString());
        this.measureDataService.getMeasureDataList(params).subscribe({
            next: measureDataResponse => {
                this.measureDataList = measureDataResponse.data
                this.dataSource.data = measureDataResponse.data
                this.dataSource.sort = this.sort
                console.log("Datasource on getMeasureDataList: ", this.dataSource)

                this.calendarId = measureDataResponse.calendarId;
                this.measureDataList = measureDataResponse.data;
                this.dataRange = measureDataResponse.range;
                this.allow = measureDataResponse.allow;
                this.locked = measureDataResponse.locked;
                this.editValue = measureDataResponse.editValue;
                this.showActionButtons = this.allow && !this.locked;
                this.measureDataResponse = measureDataResponse;
                this.disabledAll = false;
                //this.loadTable();
                this.progress(false);
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

    //-----------Selection Calculated
    selCalculated = [
        { id: 0, name: "Manual and Calculated" },
        { id: 1, name: "Manual" },
        { id: 2, name: "Calculated" }
    ];

    itgSelCal = { calculated: 0, manual: 1, all: 2 };

    onSelCalChange(): void {
        console.log("onSelCalChange: ", this.model.selCalSelected);

        if (this.model.selCalSelected == 2) {
            this.dataSource.data = this.measureDataList.filter(item => item.calculated);
            //console.log("onSelCalChange calculated length: ", this.dataSource.data.length);
            //console.log("onSelCalChange calculated: ", this.dataSource.data);
        }
        if (this.model.selCalSelected == 1) {
            this.dataSource.data = this.measureDataList.filter(item => !item.calculated);
            //console.log("onSelCalChange manual lenght: ", this.dataSource.data.length);
            //console.log("onSelCalChange manual: ", this.dataSource.data);
        }
        if (this.model.selCalSelected == 0) {
            this.dataSource.data = this.measureDataList
            //console.log("onSelCalChange both lenght: ", this.dataSource.data.length);
            //console.log("onSelCalChange both: ", this.dataSource.data);
        }
        this.dataSource._updateChangeSubscription();
        console.log("Datasource on onSelCalChange: ", this.dataSource.data)
    }

    // -----------------------------------------------------------------------------
    // Buttons
    // -----------------------------------------------------------------------------

    refresh() {
        //if ( !itgIsNull(filteredPage) ){
        if (this.filterSelected) {
            this.loadTable();
        }
    }

    edit(data: any) {
        if (!this.allow || this.locked) { return; }

        this.disabledAll = true;
        const id = data.id;

        if (this.editValue && !data.calculated) {
            this.editBgColor = false;
        }
        const idB = document.querySelector(`.tdB${ id }`);
        if (idB) {
            idB.innerHTML = '';
            idB.innerHTML = `<textarea class="mExp${ id }" rows="2" maxlength="300">${ this.itgStrNullToEmpty(data.explanation) }</textarea>`;
        }
        const idC = document.querySelector(`.tdC${ id }`);
        if (idC) {
            idC.innerHTML = '';
            idC.innerHTML = `<textarea class="mAct${ id }" rows="2" maxlength="300">${ this.itgStrNullToEmpty(data.action) }</textarea>`;
        }
    }

    edit2(data: any) {
        //this.disabledAll = true;
        this.editValue = true;
        console.log("edit2");
        console.log("edit2 data: ", data);
        console.log("edit2 editValue: ", this.editValue);
        if (!this.allow || this.locked) { return; }
        console.log("edit2 data: ", data);
        this.disabledAll = false;
        const id = data.id;

        if (this.editValue && !data.calculated) {
            this.editBgColor = false;
        }

        const idB = document.querySelector(`.tdB${ id }`);
        if (idB) {
            idB.innerHTML = '';
            idB.innerHTML = `<textarea class="mExp${ id }" rows="2" maxlength="300">${ this.itgStrNullToEmpty(data.explanation) }</textarea>`;
        }
        const idC = document.querySelector(`.tdC${ id }`);
        if (idC) {
            idC.innerHTML = '';
            idC.innerHTML = `<textarea class="mAct${ id }" rows="2" maxlength="300">${ this.itgStrNullToEmpty(data.action) }</textarea>`;
        }
    }



    cancel(data: any) {
        this.disabledAll = false;
        this.editValue = false;
        const id = data.id;
        console.log("cancel");
        console.log("cancel data: ", data);
        console.log("cancel editValue: ", this.editValue);

        if (this.editValue && !data.calculated) {
            if (this.editValue) {
                this.editBgColor = true;
                const idA = document.querySelector(`.tdA${ id }`);
                if (idA) {
                    idA.classList.add(this.getBgColor(data));
                    idA.innerHTML = `<span>${ this.itgStrNullToEmpty(data.value) }</span>`;
                }
            }
        }
        const idB = document.querySelector(`.tdB${ id }`);
        if (idB) {
            idB.innerHTML = `<span>${ this.itgStrNullToEmpty(data.explanation) }</span>`;
        }
        const idC = document.querySelector(`.tdC${ id }`);
        if (idC) {
            idC.innerHTML = `<span>${ this.itgStrNullToEmpty(data.action) }</span>`;
        }
    }

    save(data: any): void {
        if (!this.allow || this.locked) {
            return;
        }

        this.showError = false;
        this.disabledAll = true;
        let msg = '';
        const id = data.id;
        let mVal = data.value;

        if (this.editValue && !data.calculated) {
            const mVal = (<HTMLInputElement>document.querySelector('.mVal' + id)).value;
            if (!this.itgIsEmpty(mVal)) {
                if (!this.itgIsNumeric(mVal)) {
                    (<HTMLInputElement>document.querySelector('.mVal' + id)).focus();
                    const msg = 'Measure Value must be a Number.';
                    //return dialog.alert(this.title, msg);
                };
            }
        }

        const mExp = (<HTMLInputElement>document.querySelector('.mExp' + id)).value.trim();
        const mAct = (<HTMLInputElement>document.querySelector('.mAct' + id)).value.trim();

        data.explanation = this.itgStrNullToEmpty(data.explanation);
        data.action = this.itgStrNullToEmpty(data.action);

        const dataNew = { ...data };

        if (this.itgIsEmpty(mVal)) {
            dataNew.value = null;
        } else {
            dataNew.value = Number(mVal);
        }

        dataNew.explanation = mExp;
        dataNew.action = mAct;

        // if (this.isEqual(data, dataNew)) {
        //   msg =
        //     "There are no changes for <br /> '" +
        //     dataNew.name +
        //     "'.<br /> Unable to Save.";
        //   return dialog.alert(this.title, msg);
        // }

        // Call Server - PUT
        this.progress(true);

        // this.pages.measureData
        //   .update(
        //     {
        //       calendarId: this.calendarId,
        //       day: null,
        //       hierarchyId: this.hierarchyId,
        //       measureTypeId: this.measureTypeId,
        //       measureDataId: data.id,
        //       measureValue: mVal,
        //       explanation: mExp,
        //       action: mAct,
        //     },
        //     (value) => {
        //       if (itgIsNull(value.error) && value.data.length > 0) {
        //         data.value = value.data[0].value;
        //         data.explanation = value.data[0].explanation;
        //         data.action = value.data[0].action;
        //         data.updated = value.data[0].updated;
        //         //logger.logSuccess('Measure ' + data.name + ' updated.');
        //         this.progress(false);
        //         this.cancel(data);
        //       } else {
        //         this.processLocalError(
        //           this.title,
        //           value.error.message,
        //           value.error.id,
        //           null,
        //           value.error.authError
        //         );
        //       }
        //       this.disabledAll = false;
        //     },
        //     (err: { statusText: string; status: number | null; }) => {
        //       this.processLocalError(this.title, err.statusText, null, err.status, null);
        //     }
        //   );

        this.cancel(data);
    }







    doEditType() {
        this.drawer = { title: "Edit Measure Type", button: "Save", position: "end" }
        this.editingMeasureType = { ...this.selectedMeasureType }
    }

    identity(index: number, item: any) {
        return item.id
    }

    onEdit(element: MeasureDataDto) {
        this.isEditMode = true;
        // this.selectedRow = { ...targetRow };
        this.selectedRow = element;
    }

    onSave(measureDataRow: MeasureDataDto) {
        this.isEditMode = false
        //this.selectedRow = { ...this.measureDataRow };
        this.selectedRow = { ...measureDataRow };
    }

    onCancel(targetRow: MeasureDataDto) {
        this.isEditMode = false
    }

    doFilter() {
        this.drawer = { title: "Filter", button: "Apply", position: "start" }
    }

    isBoolShow(str: string | boolean): boolean {
        return ((str === "true") || (str === true));
    }

    toggleFilterOpen(): void {
        // toggle filter side nav
    }

    progress(bool: boolean): void {
    }

    closeError(): void {
        this.errorMsg = "";
        this.showError = false;
    }

    processLocalError(title: string, message: string, id: null | number, status: null | number, authError: boolean | null): void {
        this.errorMsg = this.processError(title, message, id, status);
        this.progress(false);
        this.disabledAll = false;
        this.showContentPage = (authError !== true);
    }

    processError(title: string, message: string, id: number | null, status: number | null): string {
        return title + message  // TODO: finish
    }


    //================================================================================================
    // Called from FilterCtrl only


    getData(filtered: any) {
        this.showError = false;
        this.disabledAll = true;
        this.dataRange = "";

        this.filteredPage = filtered;
        this.calendarId = filtered.calendarId;
        //this.day = filtered.day;
        this.hierarchyId = filtered.hierarchyId;
        this.measureTypeId = filtered.measureTypeId;
        this.progress(true);
        this.measureDataService.getMeasureDataList(filtered).subscribe({
            next: response => {
                if (this.itgIsNull(response.error)) {
                    this.calendarId = response.calendarId;
                    this.measureDataList = response.data;
                    this.dataRange = response.range;
                    this.allow = response.allow;
                    this.locked = response.locked;
                    this.editValue = response.editValue;
                    this.showActionButtons = this.allow && !this.locked;
                    this.measureDataResponse = response;
                    this.dataSource = new MatTableDataSource(response.data)
                    this.dataSource.sort = this.sort
                    console.log("Datasource: ", this.dataSource)
                    this.loadTable();
                    this.progress(false);
                } else {
                    this.processLocalError(this.title, response.error.message, response.error.id, null, response.error.authError);
                }
                this.disabledAll = false;
            },
            error: error => {
                this.showError = true;
                this.processLocalError(this.title, error.statusText, -99, error.status, null);
            }
        })
        //console.log(object);
        //this.loadTable();
    }

    // -----------------------------------------------------------------------------
    // Styles
    // -----------------------------------------------------------------------------

    // Measure Value Bg colors
    getBgColor(data: any): string {
        // if ( !this.editBgColor ) return "";
        // if ( itgIsEmpty(data.value) ) return "";
        // if ( itgIsNull(data.target) && itgIsNull(data.yellow) ) return "";
        if (!this.editBgColor) {
            return "";
        }

        if (!data.value) {
            return "";
        }

        if (!data.target && !data.yellow) {
            return "";
        }

        const red = "bg-danger2";
        const yellow = "bg-warning2";
        const green = "bg-success2";
        let result = red;

        if (!data.target) {
            if (data.value >= data.yellow) {
                return green;
            }
            return result;
        }
        if (!data.yellow) {
            if (data.value >= data.target) {
                return green;
            }
            return result;
        }
        if (data.target >= data.yellow) {
            if (data.value >= data.yellow) {
                result = yellow;
            }
            if (data.value >= data.target) {
                result = green;
            }
        }
        if (data.target < data.yellow) {
            if (data.value <= data.yellow) {
                result = yellow;
            }
            if (data.value <= data.target) {
                result = green;
            }
        }
        return result;
    }

    getBorderColor(targetVal: any, yellowVal: any): void {
        const red = 'border-danger2';
        const yellow = 'border-warning2';
        const green = 'border-success2';

        const mVal = document.querySelectorAll('.mVal');
        mVal.forEach(elem => elem.classList.remove(red, yellow, green));

        if (this.itgIsEmpty(mVal)) {
            return;
        }

        if (this.itgIsNull(targetVal)) {
            if (mVal >= yellowVal) {
                mVal.forEach(elem => elem.classList.add(green));
                return;
            }
            mVal.forEach(elem => elem.classList.add(red));
            return;
        }

        if (this.itgIsNull(yellowVal)) {
            if (mVal >= targetVal) {
                mVal.forEach(elem => elem.classList.add(green));
                return;
            }
            mVal.forEach(elem => elem.classList.add(red));
            return;
        }

        if (targetVal >= yellowVal) {
            if (mVal >= targetVal) {
                mVal.forEach(elem => elem.classList.add(green));
                return;
            }
            if (mVal >= yellowVal) {
                mVal.forEach(elem => elem.classList.add(yellow));
                return;
            }
        }
        if (targetVal < yellowVal) {
            if (mVal <= targetVal) {
                mVal.forEach(elem => elem.classList.add(green));
                return;
            }
            if (mVal <= yellowVal) {
                mVal.forEach(elem => elem.classList.add(yellow));
                return;
            }
        }
        mVal.forEach(elem => elem.classList.add(red));
    }


    // -----------------------------------------------------------------------------
    // Utils
    // -----------------------------------------------------------------------------

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