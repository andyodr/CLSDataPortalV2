import { animate, state, style, transition, trigger } from '@angular/animations';
import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, _SnackBarContainer } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { filter, Subscription } from 'rxjs';
import { AppDialog } from '../app-dialog.component';
import { processError } from '../lib/app-constants';
import { Hierarchy, RegionFilter } from '../_services/hierarchy.service';
import { Target, MeasureType, TargetApiParams, TargetApiResponse, TargetFilter, TargetDto, ConfirmIntervals } from '../_models/target';
import { LoggerService } from '../_services/logger.service';;
import { TargetService } from '../_services/target.service';

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

    targetResponse: TargetApiResponse | undefined;
    @Output() progressEvent = new EventEmitter<boolean>();

    title = "Targets"
    //filters: any = null;
    filters!: TargetFilter
    filtersSelected: string[] = []
    dataSource = new MatTableDataSource([] as Target[])
    //dataSource = new MatTableDataSource<Target>()
    //dataSource = new MatTableDataSource<Data>()
    displayedColumns = ["name", "value", "yellow", "updated", "actions"]
    expandDetail = new ToggleQuery()
    @ViewChild(MatSort) sort!: MatSort
    //----------------
    isEditMode = false
    selectedRow: Target | undefined
    //isEditable: boolean[] = Array(5).fill(false);
    //----------------

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

    data: Target [] = [];

    //----------------
    //TargetDto Model
    //----------------
    // targetDto: TargetDto = {
    //     hierarchyId: null,
    //     measureTypeId: null,
    //     measureId: null,
    //     target: null,
    //     yellow: null,
    //     applyToChildren: false,
    //     isCurrentUpdate: false,
    //     confirmIntervals: null
    // };
    //----------------
    //TargetDto Separate Properties
    //----------------
    hierarchyId: number | null = null;
    measureTypeId: number | null = null;
    measureId: number | null = null
    target: number | null = null
    yellow: number | null = null
    applyToChildren: boolean | null = null
    isCurrentUpdate: boolean | null = null
    confirmIntervals: ConfirmIntervals | null = null
    //----------------
    //confirmIntervals: any;
    //----------------
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
    filtered: TargetApiParams = {
        hierarchyId: 1,
        measureTypeId: 1,
    }


    constructor(private targetService: TargetService, public logger: LoggerService, private dialog: MatDialog) { }

    ngOnInit(): void {

        //console.log("target init");
        //this.targetService.getTarget()
        //this.getTargets(this.filtered)
        this.targetService.getTargetFilter().subscribe({
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
                this.getTargets(this.filtered);
            }
        })
    }

    loadTable() {
        this.filtersSelected = [this.selectedMeasureType.name]
        this.targetService.getTarget2(this.filtered).subscribe({
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

    onEdit(element: Target) {
        this.isEditMode = true;
        // this.selectedRow = { ...targetRow };
        this.selectedRow = element;
    }

    onSave(targetRow: Target) {
        this.isEditMode = false
        this.selectedRow = { ...targetRow };
    }

    onCancel(targetRow: Target) {
        this.isEditMode = false
    }

    save() {
        this.loadTable()
    }

    applyFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.dataSource.filter = filterValue.trim().toLowerCase()
    }

    identity(_: number, item: Target) {
        return item.id
    }

    closeError() {
        this.errorMsg = ""
        this.showError = false
    }

    getTargets(filtered: TargetApiParams): void {
        this.showError = false;
        this.disabledAll = true;
        //this.data = null;
        console.log("get target on the component");
        // Call Server
        this.targetService.getTarget2(filtered).subscribe({
            next: response => {
            this.targetResponse = response;
            this.data = response.data
            console.log("Target on Component: ", response);
            //this.dataSource = new MatTableDataSource(response.data)
            //console.log("Datasource: ", this.dataSource);
            //this.dataSource.sort = this.sort
            this.allow = response.allow
            this.confirmed = response.confirmed
            this.dataSource = new MatTableDataSource(response.data)
            this.dataSource.sort = this.sort
            console.log("Datasource: ", this.dataSource)
            this.disabledAll = false;
            },
            error: (err: any) => {
                this.processLocalError(this.title, err.error.message, err.error.id, null, err.error.authError)
            }
        })
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
        this.showError = false;
        this.disabledAll = true;
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

    confirmTarget(): void {
        const dialogRef = this.dialog.open(AppDialog);
        dialogRef.afterClosed().subscribe((result) => {
            if (result === 'confirmCurrentSave') {
                this.dataConfirmed.isCurrentUpdate = true;
                //this.dataConfirmed.confirmIntervals = this.confirmIntervals;
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
            // measureId: null,
            // target: null,
            // yellow: null,
            measureId: this.measureId ?? undefined,
            target: this.target ?? undefined,
            yellow: this.yellow ?? undefined,
            applyToChildren: true,
            isCurrentUpdate: this.dataConfirmed.isCurrentUpdate,
            confirmIntervals: this.dataConfirmed.confirmIntervals,
        }

        this.targetService.applyTargetToChildren(targetDtoApplyChildrenSave).subscribe({
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

        this.targetService.updateTarget(this.dataConfirmed.id, targetDtoSave).subscribe({
            next: value => {
                if (!(value.error) && value.data.length > 0) {
                    this.dataConfirmed.data.target = value.data[0].target;
                    this.dataConfirmed.data.yellow = value.data[0].yellow;
                    this.dataConfirmed.data.targetId = value.data[0].targetId;
                    this.dataConfirmed.data.targetCount = value.data[0].targetCount;
                    this.dataConfirmed.data.updated = value.data[0].updated;
                    this.logger.logSuccess('Measure ' + this.dataConfirmed.data.name + ' updated.');
                    this.logger.logSuccess('Success: Target saved.');
                    //this.progress(false);
                    this.cancel(this.dataConfirmed.data);
                } else {
                    this.logger.logError('Error: Target not saved.');
                }
                //this.progress(false);
            },
            error: err => this.processLocalError(this.title, err.statusText, null, err.status, null),
            complete: () => { this.disabledAll = false; }
        });
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
