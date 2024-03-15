import { animate, state, style, transition, trigger } from '@angular/animations';
import { Component, OnInit, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSort, MatSortModule } from "@angular/material/sort"
import { MatTableDataSource, MatTableModule } from "@angular/material/table"
import { finalize } from "rxjs"
import { AppDialog } from '../app-dialog.component';
import { RegionFilter } from "../_services/hierarchy.service"
import { MeasureType, TargetApiResponse, TargetFilterResponseDto, TargetDto, ConfirmIntervals, TargetApiParams, TargetFilter } from '../_models/target';
import { LoggerService } from "../_services/logger.service"
import { TargetService } from '../_services/target.service';
import { RegionTreeComponent } from '../lib/region-tree/region-tree.component';
import { HttpParams } from '@angular/common/http';
import { AccountService } from "../_services/account.service"
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
    selector: 'app-targets',
    templateUrl: './targets.component.html',
    styleUrls: ['./targets.component.scss'],
    animations: [
        trigger("detailExpand", [
            state("false", style({ height: "0px", minHeight: "0" })),
            state("true", style({ height: "*" })),
            transition("true <=> false", animate("225ms cubic-bezier(0.4, 0.0, 0.2, 1)"))
        ])
    ],
    standalone: true,
    imports: [MatProgressBarModule, MatSidenavModule, MatButtonModule, MatIconModule, FormsModule, MatFormFieldModule, MatSelectModule, MatOptionModule, RegionTreeComponent, SidebarComponent, ErrorsComponent, NgbAlert, MatInputModule, MatTableModule, MatSortModule]
})
export class TargetsComponent implements OnInit {

    title = "Targets"
    targetResponse: TargetApiResponse | undefined;

    //Filter Properties
    drawer = {
        title: "Filter",
        button: "Apply",
        position: "start" as "start" | "end"
    }
    filters!: TargetFilter
    //filters!: TargetFilterResponseDto
    filtersSelected: string[] = []
    hierarchy: RegionFilter[] = []
    selectedRegion = null as number | number[] | null

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
    // selectedMeasureType?: MeasureType
    selectedHierarchy = null as number | number[] | null

    //Table Properties
    targetList: TargetDto[] = [];
    selectedRow: TargetDto | undefined
    dataSource = new MatTableDataSource<TargetDto>()
    displayedColumns = ["name", "target", "yellow", "updated", "actions"]
    expand = new ToggleQuery()
    @ViewChild(MatSort) sort!: MatSort
    editingMeasureType!: any
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
    // editValue: boolean | undefined;
    // showActionButtons: boolean | undefined;
    locked: boolean | undefined
    calendarId!: number;
    hierarchyId!: number;
    measureTypeId!: number;

    measureTypes: MeasureType[] = []
    targetRow: TargetDto | undefined

    measureId: number | null = null
    skTargets = "";
    confirmed = false;


    target: number | null = null //or this.model.target
    yellow: number = 0 //or this.model.yellow
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

    //Model
    model = {
        value: 0,
        target: 0, //or this.target
        yellow: 0, //or this.target
        measureType: 0,
        selectedRegion: null as number | number[] | null,
    }

    //Error handling within the component
    errorMsg: any = ""
    showError: boolean = false;

    constructor(private targetSvc: TargetService, public acctSvc: AccountService, public logger: LoggerService, private dialog: MatDialog) { }

    ngOnInit(): void {
        this.progress = true
        this.targetSvc.getTargetFilter()
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: dtofilter => {
                    this.filters = dtofilter;
                    this.measureTypes = dtofilter.measureTypes
                    this.hierarchy = dtofilter.hierarchy
                    this.selectedRegion = dtofilter.filter.hierarchyId ?? dtofilter.hierarchy.at(0)?.id ?? 1

                    //const { hierarchyId, measureTypeId } = dtofilter.filter
                    const userSettings = this.acctSvc.getCurrentUser()?.filter
                    let hierarchyId = userSettings?.hierarchyId
                    hierarchyId ||= dtofilter.filter.hierarchyId || dtofilter.hierarchy.at(0)?.id || 1
                    let measureTypeId = userSettings?.measureTypeId
                    measureTypeId ||= dtofilter.filter.measureTypeId
                    this.selectedMeasureType = this.measureTypes.find(t => t.id == measureTypeId) ?? this.measureTypes[0]
                    this.selectedHierarchy = hierarchyId
                    setTimeout(() => this.loadTable())
                }
            })
    }

    ngAfterViewInit() {
        this.dataSource.sort = this.sort;
        this.dataSource.sortingDataAccessor = (item, property) => {
            switch (property) {
                case 'name': return item.name;
                case 'target': return item.target;
                case 'yellow': return item.yellow;
                case 'updated': return item.updated.longDt;
                default: return (item as any)[property];
            }
        };
    }

    // -----------------------------------------------------------------------------
    // Load Table Data
    // -----------------------------------------------------------------------------
    loadTable(): void {
        if (!this.selectedMeasureType || typeof this.selectedHierarchy !== "number") return

        //this.filtersSelected = [ this.selectedMeasureType.name, this.tree.ancestorPath.join(" | ") ]

        const params = { measureTypeId: 0, hierarchyId: 0 }
        params.measureTypeId = this.selectedMeasureType.id
        params.hierarchyId = this.selectedHierarchy
        this.acctSvc.saveSettings({
            measureTypeId: this.selectedMeasureType.id,
            hierarchyId: this.selectedHierarchy
        })

        this.filtersDisplay = [
            this.selectedMeasureType?.name ?? "?",
            this.treeControl.ancestorPath.join(" | ")
        ]

        //this.showError = false;
        this.getTargetsList(params);
    }

    getTargetsList(parameters: { measureTypeId: number; hierarchyId: number; } | undefined) {
        this.showError = false;
        this.disabledAll = true;

        this.hierarchyId = parameters!.hierarchyId;
        this.measureTypeId = parameters!.measureTypeId;


        let params = new HttpParams()
            .set("hierarchyId", (parameters!.hierarchyId).toString())
            .set("measureTypeId", (parameters!.measureTypeId).toString())

        this.progress = true

        // Call Server - GET Target List
        this.targetSvc.getTargetList(params)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: targetResponse => {
                    this.targetResponse = targetResponse;
                    this.targetList = targetResponse.data;
                    this.dataSource.data = targetResponse.data;
                    //this.dataSource.sort = this.sort;
                    console.log("Datasource on getTargetList: ", this.dataSource)

                    this.hierarchyId = parameters!.hierarchyId;
                    this.measureTypeId = parameters!.measureTypeId;

                    this.allow = targetResponse.allow;
                    this.confirmed = targetResponse.confirmed

                    this.locked = targetResponse.locked;
                    this.editValue = targetResponse.editValue;
                    this.showActionButtons = this.allow && !this.locked;
                    this.disabledAll = false;
                    this.logger.logInfo("Target List Loaded")
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

    // -----------------------------------------------------------------------------
    // Utils
    // -----------------------------------------------------------------------------
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
        this.isEditMode = true;
        this.selectedRow = targetRow;
        // this.selectedRow = { ...targetRow };
        // this.model.target = targetRow.target;
        // this.model.yellow = targetRow.yellow;
    }

    onSave(targetRow: TargetDto) {

        // if (!this.allow || this.locked) {
        //     return;
        // }

        this.isEditMode = false
        this.selectedRow = { ...targetRow };
        // this.selectedRow = targetRow;

        this.showError = false;
        this.disabledAll = true;
        //console.log("onSave TargetRow: ", targetRow);

        const hierarchyId = this.hierarchyId;
        const measureId = targetRow.id;
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
        this.dataConfirmedReset();

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
            "confirmIntervals": {
                "daily": true,
                "weekly": true,
                "monthly": true,
                "quarterly": true,
                "yearly": true
            }
        }

        if (targetRow.target == body.target && targetRow.yellow == body.yellow) {
            this.logger.logInfo("There are no changes for " + targetRow.name + ". Unable to Save.")
            const dialogRef = this.dialog.open(AppDialog, {
                width: '450px',
                data: {
                    title: 'Alert',
                    msg: 'There are no changes for ' + targetRow.name + '. Unable to Save.'
                }
            });
        }

        this.progress = true

         // Call Server - PUT Target
        this.targetSvc.updateTarget(body)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: targetResponse => {
                    this.logger.logInfo("Measure Data Updated")
                    console.log("targetResponse on updateTarget", targetResponse);
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

        // this.model.target = null;
        // this.model.yellow = "";
        //this.loadTable()
    }

    onCancel(targetRow: TargetDto) {
        this.isEditMode = false
        this.disabledAll = false;
        // this.model.target = null;
        // this.model.yellow = "";
        this.loadTable();
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

        this.dataConfirmedReset();

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

        // Call Server - PUT Target Apply to Children
        this.targetSvc.applyTargetToChildren(body)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: value => {
                    this.logger.logSuccess('Success: Targets applied to children.');
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

        this.progress = true

        // Call Server - PUT Target
        this.targetSvc.updateTarget2(this.dataConfirmed.id, targetDtoSave)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
            next: value => {
                    if (!(value.error) && value.data.length > 0) {
                        this.dataConfirmed.data.target = value.data[0].target;
                        this.dataConfirmed.data.yellow = value.data[0].yellow;
                        this.dataConfirmed.data.targetId = value.data[0].targetId;
                        this.dataConfirmed.data.targetCount = value.data[0].targetCount;
                        this.dataConfirmed.data.updated = value.data[0].updated;
                        this.logger.logSuccess('Measure ' + this.dataConfirmed.data.name + ' updated.');
                        this.logger.logSuccess('Success: Target saved.');
                        this.cancel(this.dataConfirmed.data);
                    } else {
                        this.logger.logError('Error: Target not saved.');
                    }
                },
                error: err => this.processLocalError(this.title, err.statusText, null, err.status, null),
                complete: () => { this.disabledAll = false; }
            });
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
    // From Previous Code
    // -----------------------------------------------------------------------------

    edit(data: any) {
        this.skTargets = "edit";
    }
    save(data: any) {
        this.skTargets = "save";
    }
    cancel(data: any) {
        this.skTargets = "cancel";
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
