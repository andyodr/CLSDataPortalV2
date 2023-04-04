import { animate, state, style, transition, trigger } from '@angular/animations';
import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, _SnackBarContainer } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { filter, finalize, Subscription } from 'rxjs';
import { AppDialog } from '../app-dialog.component';
import { processError } from '../lib/app-constants';
import { Hierarchy, RegionFilter } from '../_services/hierarchy.service';
import { MeasureType, TargetApiResponse, TargetFilterResponseDto, TargetDto, ConfirmIntervals, TargetApiParams, TargetFilter } from '../_models/target';
import { LoggerService } from '../_services/logger.service';;
import { TargetService } from '../_services/target.service';
import { RegionTreeComponent } from '../lib/region-tree/region-tree.component';
import { HttpParams } from '@angular/common/http';

@Component({
    selector: 'app-targets',
    templateUrl: './targets.component.html',
    styleUrls: ['./targets.component.scss'],
    animations: [
        trigger("detailExpand", [
            state("false", style({ height: "0px", minHeight: "0" })),
            state("true", style({ height: "*" })),
            transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
        ])]
})
export class TargetsComponent implements OnInit {

    title = "Targets"
    targetResponse: TargetApiResponse | undefined;
    filters!: TargetFilter
    filtersSelected: string[] = []
    dataSource = new MatTableDataSource<TargetDto>()
    displayedColumns = ["name", "value", "yellow", "updated", "actions"]
    expand = new ToggleQuery()
    //@ViewChild(MatTable) matTable!: MatTable<MeasuresTableRow>
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



    @Output() progressEvent = new EventEmitter<boolean>();
    //targetResponse: TargetApiResponse | undefined;

    //----------------Table Properties----------------
    //targetList: TargetDto[] = [];
    targetRow: TargetDto | undefined
    // dataSource = new MatTableDataSource([] as TargetDto[])
    //dataSource = new MatTableDataSource<TargetDto>()
    //displayedColumns = ["name", "value", "yellow", "updated", "actions"]
    //@ViewChild(MatSort) sort!: MatSort

    //editingMeasureType!: any
    //selectedMeasureType: MeasureType = { id: 0, name: "" }


    isEditMode = false
    selectedRow: TargetDto | undefined
    expandDetail = new ToggleQuery()

    //------------------ Local Properties ------------------
    //title = "Targets"
    //showContentPage = true

    filterDisplay = {
        intervals: false,
        measureTypes: true,
        hierarchy: true
    };
    targetList: TargetDto[] = [];
    //hierarchyId: number | null = null;
    //measureTypeId: number | null = null;
    measureId: number | null = null
    //disabledAll = false
    //btnDisabled = false;
    skTargets = "";
    //allow = false;
    confirmed = false;
    //showContentPage = false

    locked: boolean | undefined;
    editValue: boolean | undefined;
    showActionButtons: boolean | undefined;

    target: number | null = null
    yellow: number = 0
    applyToChildren: boolean | null = null
    isCurrentUpdate: boolean | null = null
    confirmIntervals: ConfirmIntervals | null = null

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

    //------------------ Filter Properties ------------------
    drawer = {
        title: "Filter",
        button: "Apply",
        position: "start" as "start" | "end"
    }

    //filters!: TargetFilterResponseDto
    //filtersDisplay: string[] = []
    //@ViewChild(RegionTreeComponent) tree!: RegionTreeComponent


    @ViewChild(RegionTreeComponent) treeControl!: RegionTreeComponent
    select = {
        measureTypes: [] as MeasureType[],
        hierarchy: [] as RegionFilter[]
    }

    filtersDisplay: string[] | undefined;

    //hierarchy: RegionFilter[] = []
    hierarchyLevels!: { name: string, id: number }[]
    yearList!: { name: string, id: number }[]
    measureTypeList!: { name: string, id: number }[]
    //measureTypes: MeasureType[] = []
    //selectedMeasureType: MeasureType | undefined //= { id: 0, name: "" }
    selectedMeasureTypeId!: number
    selectedHierarchy = null as number | number[] | null
    filterSelected: string[] = []

    filtered: TargetApiParams = {
        hierarchyId: 1,
        measureTypeId: 1,
    }
    // measureTypes: MeasureType[] = []
    // selectedMeasureType?: MeasureType
    // hierarchy: RegionFilter[] = []
    // hierarchyLevels!: { name: string, id: number }[]
    // selectedHierarchy = null as number | number[] | null
    // filtered: TargetApiParams = {
    //     hierarchyId: 1,
    //     measureTypeId: 1,
    // }

    //------------------  Model ------------------
    model = {
        value: 0,
        target: 0,
        yellow: 0,
        measureType: 0,
        selectedRegion: null as number | number[] | null,
    }

    //yellow: number | null = null

    //----------------- Error handling within the component
    //errorMsg: any = ""
    //showError: boolean = false;



    constructor(private targetService: TargetService, public logger: LoggerService, private dialog: MatDialog) { }

    ngOnInit(): void {
        this.progress = true
        this.targetService.getTargetFilter()
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: dtofilter => {
                    this.filters = dtofilter;
                    this.measureTypes = dtofilter.measureTypes
                    this.selectedMeasureType = dtofilter.measureTypes[0]
                    this.hierarchy = dtofilter.hierarchy
                    this.selectedRegion = dtofilter.filter.hierarchyId ?? dtofilter.hierarchy.at(0)?.id ?? 1

                    console.log("target on init dto.filter", dtofilter.filter);
                    console.log("target on init dto", dtofilter);

                    //this.dataSource.data = dtofilter.measures.data
                    this.dataSource.sort = this.sort;

                    //const { hierarchyId, measureTypeId } = dtofilter.filter
                    const hierarchyId = dtofilter.filter.hierarchyId ?? dtofilter.hierarchy.at(0)?.id ?? 1
                    const measureTypeId = dtofilter.filter.measureTypeId ?? dtofilter.measureTypes.at(0)?.id ?? 1

                    //this.selectedMeasureType = dtofilter.measureTypes.find(t => t.id == measureTypeId)
                    this.selectedMeasureTypeId = measureTypeId
                    this.selectedHierarchy = hierarchyId
                    this.filtered.hierarchyId = hierarchyId
                    this.filtered.measureTypeId = this.selectedMeasureTypeId
                    setTimeout(() => this.loadTable())
                }
            })
    }

    loadTable(): void {

        if (!this.selectedMeasureType || typeof this.selectedHierarchy !== "number") return

        //this.filtersSelected = [ this.selectedMeasureType.name, this.tree.ancestorPath.join(" | ") ]

        const params = { measureTypeId: 0, hierarchyId: 0 }
        params.measureTypeId = this.selectedMeasureType.id
        params.hierarchyId = this.selectedHierarchy

        this.filtered.hierarchyId = this.selectedHierarchy
        this.filtered.measureTypeId = this.selectedMeasureType.id
        this.filtersDisplay = [
            this.selectedMeasureType?.name ?? "?",
            this.tree.ancestorPath.join(" | ")
        ]
        //this.showError = false;
        //this.disabledAll = true;
        this.getTargetsList(params);
    }

    //----------------getTargetsList----------------
    getTargetsList(parameters: { measureTypeId: number; hierarchyId: number; } | undefined) {
        this.showError = false;
        this.disabledAll = true;

        this.hierarchyId = parameters!.hierarchyId;
        this.measureTypeId = parameters!.measureTypeId;

        this.progress = true
        let params = new HttpParams()
            .set("hierarchyId", (parameters!.hierarchyId).toString())
            .set("measureTypeId", (parameters!.measureTypeId).toString())
        this.targetService.getTargetList(params)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: targetResponse => {
                    this.targetResponse = targetResponse;
                    this.targetList = targetResponse.data;
                    this.dataSource.data = targetResponse.data;
                    this.dataSource.sort = this.sort;
                    console.log("Target List on getTargetList: ", this.targetList)
                    console.log("Datasource on getTargetList: ", this.dataSource)

                    // this.hierarchyId? = parameters?.hierarchyId;
                    // this.measureTypeId? = parameters?.measureTypeId;

                    this.allow = targetResponse.allow;
                    this.confirmed = targetResponse.confirmed

                    this.locked = targetResponse.locked;
                    this.editValue = targetResponse.editValue;
                    this.showActionButtons = this.allow && !this.locked;
                    this.disabledAll = false;

                    //this.confirmIntervals = targetResponse.confirmIntervals;

                    //this.progress(false);
                    this.logger.logInfo("Target List Loaded")
                },
                error: err => {
                    this.logger.logError(err.message)
                    this.errorMsg = err
                    this.showError = true
                    this.processLocalError(this.title, err.statusText, null, err.status, null);
                    //this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
                }
            })
    }

    applyTableFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
    }

    dataConfirmedReset(): void {
        this.dataConfirmed = {
            target: null,
            yellow: null,
            data: null,
            isApplyToChildren: false,
            isCurrentUpdate: false,
            confirmIntervals: null,
            targetId: null,
            targetCount: null
        };
    }

    identity(index: number, item: any) {
        return item.id
    }

    // -----------------------------------------------------------------------------
    // Buttons
    // -----------------------------------------------------------------------------

    onEdit(targetRow: TargetDto) {

        // if (!this.allow || this.locked) {
        //     return;
        // }

        //this.selectedRow = { ...targetRow };
        // this.selectedRow = targetRow;
        // console.log("Selected Row: ", this.selectedRow)
        // if (targetRow.value !== undefined) {
        //     this.model.value = targetRow.target;
        // }
        //this.model.value = targetRow.target;
        //this.model.yellow = targetRow.yellow;
        this.isEditMode = true;
        this.selectedRow = targetRow;
        this.model.target = targetRow.target;
        this.model.yellow = targetRow.yellow;
    }

    onCancel(targetRow: TargetDto) {
        this.isEditMode = false
        this.loadTable();
    }

    onSave(targetRow: TargetDto) {

        this.isEditMode = false
        //this.showError = false;
        //this.disabledAll = true;

        this.selectedRow = targetRow;

        console.log("onSave TargetRow: ", targetRow);

        // if (!this.allow || this.locked) {
        //     return;
        // }

        //const measureDataRowNew = { ...measureDataRow };

        const hierarchyId = this.hierarchyId;
        //const measureId = parseInt(targetRow.id);
        const measureId = targetRow.id;
        const measureTypeId = this.measureTypeId;
        const target = this.model.value;
        // const yellow = this.model.yellow;
        const applyToChildren = true;
        const isCurrentUpdate = true;
        const confirmIntervals = {
            daily: true,
            weekly: true,
            monthly: true,
            quarterly: true,
            yearly: true
        }


        //this.dataConfirmedReset();

        // Check if values are null and convert it to a default value if necessary
        const fixedHierarchyId = hierarchyId === null ? 0 : hierarchyId;
        const fixedmeasureTypeId = measureTypeId === null ? 0 : measureTypeId;

        const body = {
            "hierarchyId": fixedHierarchyId,
            "measureId": measureId,
            "measureTypeId": fixedmeasureTypeId,
            "target": this.model.value,
            "yellow": this.yellow,
            "applyToChildren": false,
            "isCurrentUpdate": false,
            //"confirmIntervals": null
            "confirmIntervals": {
                "daily": true,
                "weekly": true,
                "monthly": true,
                "quarterly": true,
                "yearly": true
            }
        }

        // if (targetRow.target == body.target && targetRow.yellow == body.yellow) {
        //     this.logger.logInfo("There are no changes for " + targetRow.name + ". Unable to Save.")
        //     const dialogRef = this.dialog.open(AppDialog, {
        //         width: '450px',
        //         data: {
        //             title: 'Alert',
        //             msg: 'There are no changes for ' + targetRow.name + '. Unable to Save.'
        //         }
        //     });
        // }

        // Call Server - PUT
        this.progress = true
        //console.log("body for updateTarget", body);
        console.log("fixedHierarchyId onSaveTarget", fixedHierarchyId);

        console.log("body on updateTarget", body);


        this.targetService.updateTarget(body)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: targetResponse => {
                    this.logger.logInfo("Measure Data Updated")
                    console.log("targetResponse on updateTarget", targetResponse);
                    //this.progress(false);
                    //this.disabledAll = false;
                    //this.loadTable();
                    //this.dataSource.data = targetResponse.data;
                },
                error: err => {
                    this.logger.logError(err.message)
                    this.errorMsg = err
                    this.showError = true
                    this.processLocalError(this.title, err.statusText, null, err.status, null);
                }
            })

        this.loadTable()

    }

    save() {
        this.loadTable()
    }


    closeError() {
        this.errorMsg = ""
        this.showError = false
    }

    edit(data: any) {
        this.skTargets = "edit";
    }
    save1(data: any) {
        this.skTargets = "save";
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


    applyToChildrenAction(): void {
        // if (!this.allow) {
        //   return;
        // }
        // this.showError = false;
        // this.disabledAll = true;
        const dialogRef = this.dialog.open(AppDialog, {
            width: '450px',
            data: {
                title: 'Confirm',
                msg: 'Are you sure you want to apply to all children?'
            }
        });

        dialogRef.afterClosed().subscribe(result => {
            console.log('The dialog was closed');
            if (result) {
                this.dataConfirmedReset();
                this.dataConfirmed.isApplyToChildren = true;

                if (this.confirmed) {
                    this.confirmTarget();
                } else {
                    this.applyToChildrenSave();
                }
            } else {
                this.disabledAll = false;
                this.dataConfirmedReset();
                this.dataConfirmed.isApplyToChildren = true;
            }
        });
    }



    confirmTarget(): void {
        const dialogRef = this.dialog.open(AppDialog);
        dialogRef.afterClosed().subscribe((result) => {
            if (result === 'confirmCurrentSave') {
                this.dataConfirmed.isCurrentUpdate = true;
                this.dataConfirmed.confirmIntervals = this.confirmIntervals;
                if (this.dataConfirmed.isApplyToChildren) {
                    this.applyToChildrenSave();
                } else {
                    this.saveCall();
                }
            } else if (result === 'confirmFutureSave') {
                if (this.dataConfirmed.isApplyToChildren) {
                    this.applyToChildrenSave();
                } else {
                    this.saveCall();
                }
            } else {
                this.disabledAll = false;
            }
        });
    }

    applyToChildrenSave(): void {
        // Call Server - PUT
        //this.progress(true);
        const targetDtoApplyChildrenSave = {
            hierarchyId: this.hierarchyId ?? undefined,
            measureTypeId: this.measureTypeId ?? undefined,
            measureId: this.measureId ?? undefined,
            target: this.target ?? undefined,
            yellow: this.yellow ?? undefined,
            applyToChildren: true,
            isCurrentUpdate: this.dataConfirmed.isCurrentUpdate,
            confirmIntervals: this.dataConfirmed.confirmIntervals,
        }

        const hierarchyId = this.hierarchyId;
        const measureId = this.measureId ?? undefined
        const measureTypeId = this.measureTypeId;
        const target = this.model.value;
        const yellow = this.model.yellow;
        const applyToChildren = true;
        const isCurrentUpdate = true;
        const confirmIntervals = {
            daily: true,
            weekly: true,
            monthly: true,
            quarterly: true,
            yearly: true
        }

        //this.dataSource.a



        //this.dataConfirmedReset();

        // Check if values are null and convert it to a default value if necessary
        const fixedHierarchyId = hierarchyId === null ? 0 : hierarchyId;
        const fixedMeasureId = measureId === null ? 0 : measureId;
        const fixedmeasureTypeId = measureTypeId === null ? 0 : measureTypeId;

        const body = {
            "hierarchyId": fixedHierarchyId,
            "measureId": fixedMeasureId,
            "measureTypeId": fixedmeasureTypeId,
            "target": this.model.value,
            "yellow": this.model.yellow,
            "applyToChildren": true,
            "isCurrentUpdate": this.dataConfirmed.isCurrentUpdate,
            "confirmIntervals": this.dataConfirmed.confirmIntervals
        }

        this.progress = true
        this.targetService.applyTargetToChildren(body)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: value => {
                    this.logger.logSuccess('Success: Targets applied to children.');
                    //this.progress(false);
                },
                error: err => this.processLocalError(this.title, err.statusText, null, err.status, null),
                complete: () => { this.disabledAll = false; }
            });
    }

    saveCall(): void {
        // Call Server - PUT
        //this.progress(true);

        const targetDtoSave = {
            hierarchyId: this.hierarchyId ?? undefined,
            measureTypeId: this.measureTypeId ?? undefined,
            measureId: this.dataConfirmed.data.id,
            target: this.dataConfirmed.target,
            yellow: this.dataConfirmed.yellow,
            applyToChildren: false,
            isCurrentUpdate: this.dataConfirmed.isCurrentUpdate,
            confirmIntervals: this.dataConfirmed.confirmIntervals,
        }

        // this.targetService.updateTarget2(this.dataConfirmed.id, targetDtoSave).subscribe({
        //     next: value => {
        //         if (!(value.error) && value.data.length > 0) {
        //             this.dataConfirmed.data.target = value.data[0].target;
        //             this.dataConfirmed.data.yellow = value.data[0].yellow;
        //             this.dataConfirmed.data.targetId = value.data[0].targetId;
        //             this.dataConfirmed.data.targetCount = value.data[0].targetCount;
        //             this.dataConfirmed.data.updated = value.data[0].updated;
        //             this.logger.logSuccess('Measure ' + this.dataConfirmed.data.name + ' updated.');
        //             this.logger.logSuccess('Success: Target saved.');
        //             //this.progress(false);
        //             this.cancel(this.dataConfirmed.data);
        //         } else {
        //             this.logger.logError('Error: Target not saved.');
        //         }
        //         //this.progress(false);
        //     },
        //     error: err => this.processLocalError(this.title, err.statusText, null, err.status, null),
        //     complete: () => { this.disabledAll = false; }
        // });
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
