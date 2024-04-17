import { formatDate, DatePipe } from "@angular/common"
import { HttpClient, HttpErrorResponse, HttpParams } from "@angular/common/http"
import { Component, DestroyRef, Inject, LOCALE_ID, Signal, ViewChild, inject } from "@angular/core"
import { MatDialog } from "@angular/material/dialog"
import { Intervals, LINE1, LINE2, MESSAGES, processError } from "../lib/app-constants"
import { environment } from "../../environments/environment"
import { FilterPipe } from "../lib/filter.pipe"
import { TableComponent, JsonValue } from "../lib/table/table.component"
import { AppDialog } from "../app-dialog.component"
import { MultipleSheetsDialog } from "./multiplesheets-dialog.component"
import { WorkBook, read, utils } from 'xlsx'
import { ErrorModel } from "../_models/error"
import { LoggerService } from "../_services/logger.service"
import { MeasureDataService } from "../_services/measure-data.service"
import { IntervalDto } from "../_services/measure-definition.service"
import { FiltersIntervalsData } from "../_models/measureData"
import { ErrorsComponent } from "../errors/errors.component"
import { UploadDirective } from "./upload.directive"
import { MatInputModule } from "@angular/material/input"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatOptionModule } from "@angular/material/core"
import { FormsModule } from "@angular/forms"
import { MatSelectModule } from "@angular/material/select"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatProgressBarModule } from "@angular/material/progress-bar"
import { SidebarComponent } from "../nav/sidebar.component"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { finalize } from "rxjs"
import { AccountService } from "../_services/account.service"
import packageJson from "../../../package.json"

type DataOut = {
    dataImport: number
    calendarId?: number
    sheet: string
    data: { [name: string]: JsonValue }[]
}

type DataImportItem = {
    id: number
    name: string
    heading: {
        title: string
        required: boolean
    }[]
}

type DataImportsMainObject = {
    error: ErrorModel
    calculationTime: string
    intervals: IntervalDto[]
    years: { id: number, year: number }[]
    dataImport: DataImportItem[]
    intervalId?: number
    calendarId?: number
    currentYear?: number
}

type UploadsBody = {
    data?: DataImportsMainObject
    error: { id?: number, row?: number, message: string }[]
}

@Component({
    selector: "app-dataimports",
    templateUrl: "./dataimports.component.html",
    styleUrls: ["./dataimports.component.scss"],
    standalone: true,
    imports: [SidebarComponent, MatProgressBarModule, MatFormFieldModule, MatSelectModule, FormsModule,
        MatOptionModule, MatButtonModule, MatIconModule, MatInputModule, UploadDirective, ErrorsComponent,
        TableComponent, DatePipe]
})
export class DataImportsComponent {
    title = "Data Imports"
    version: string = packageJson.version
    apiVersion: Signal<string>
    @ViewChild(TableComponent)
    private table!: TableComponent

    showContentPage = true
    selImport: DataImportItem[] = []
    jsonObj: { [name: string]: JsonValue }[] = []
    sheetNames: { id: number, name: string }[] = []
    sheetName = ""
    colNames: string[] = []
    tableData: { [name: string]: JsonValue }[] = []
    fileName = ""
    msgUpload = ""

    showIntervals = false
    selImportSelected!: DataImportItem
    Intervals = Intervals
    fIntervals: IntervalDto[] = []
    fIntervalSelected?: IntervalDto
    fYears: { id: number, year: number }[] = []
    fYearSelected: any = []
    fMonths: FiltersIntervalsData[] = []
    fMonthSelected!: FiltersIntervalsData
    fQuarters: FiltersIntervalsData[] = []
    fQuarterSelected!: FiltersIntervalsData
    fWeeks: FiltersIntervalsData[] = []
    fWeekSelected!: FiltersIntervalsData
    calendarId!: number
    currentYear!: number
    intervalId = -1
    calculationTime: any
    disImportSel = false
    disFilters = false
    disFile = false
    disClear = false
    msgConfirm = ""

    disUpload = true
    dropDis: boolean = false
    errorUploadMsg = { heading: "", errorRows: [] as { id?: number, row?: number, message: string }[] }
    showUploadError = false
    progress = false
    errorMsg: any = ""
    showError = false
    hideTable = true

    toggle: any = true
    destroyRef = inject(DestroyRef)

    constructor(public dialog: MatDialog,
            public filterPipe: FilterPipe,
            private http: HttpClient,
            private acctSvc: AccountService,
            private api: MeasureDataService,
            private logger: LoggerService,
            @Inject(LOCALE_ID) private locale: string) {
        this.apiVersion = acctSvc.version
        this.disAll(false)
        this.showError = false
        this.disAll()

        // Call Server
        this.setProgress(true)
        http.get<DataImportsMainObject>(environment.baseUrl + "api/dataimports/index")
            .pipe(finalize(() => this.setProgress(false)), takeUntilDestroyed())
            .subscribe({
                next: dto => {
                    if (dto.error == null) {
                        this.calculationTime = dto.calculationTime
                        this.fIntervals = dto.intervals  // 2: "Weekly", 3: "Monthly", 4: "Quarterly", 5: "Yearly"
                        this.fYears = dto.years

                        // Default Interval and Calendar Id
                        this.intervalId = dto.intervalId ?? 0
                        this.calendarId = dto.calendarId ?? 0
                        this.currentYear = dto.currentYear ?? 2000
                        this.selImport = dto.dataImport
                        this.selImportSelected = this.selImport[0]
                        this.onSelImportChange()
                        this.disImportSel = this.selImport.length == 1
                    }
                    else {
                        this.processLocalError(this.title, dto.error.message, dto.error.id, null, dto.error.authError)
                    }

                    this.disAll(false)
                },
                error: (err: HttpErrorResponse) => {
                    this.processLocalError(this.title, err.error, null, err.status, null)
                }
            })
    }

    setProgress(enable: boolean) {
        this.progress = enable
    }

    closeError() {
        this.errorMsg = ""
        this.showError = false
    }

    processLocalError(name: string, message: string, id: any, status: unknown, authError: any) {
        this.errorMsg = processError(name, message, id, status)
        this.disUpload = true
        this.setProgress(false)
        this.showError = true
        this.showContentPage = (authError != true)
    }

    closeUploadError() {
        this.errorUploadMsg.heading = ""
        this.errorUploadMsg.errorRows = []
        this.showUploadError = false
    }

    processUploadError(error: { id?: number, row?: number, message: string }[]) {
        if ((error != null && "length" in error && error.length > 0)) {
            this.msgUpload = MESSAGES.uploadFailure
            this.disUpload = true

            var msg = error[0]
            if (msg.id != null) {
                this.processLocalError(this.title, msg.message, msg.id, null, null)
            }
            else {
                this.errorUploadMsg = {
                    heading: MESSAGES.uploadFailure,
                    errorRows: error
                }

                this.setProgress(false)
                this.showUploadError = true
            }
        }
    }

    showSheetDialog() {
        return this.dialog
            .open(MultipleSheetsDialog, { data: this.sheetNames })
            .afterClosed()
    }

    disAll(disable = true) {
        this.disImportSel = disable
        this.disFilters = disable
        this.disFile = disable
        this.disClear = disable
        this.dropDis = disable
        this.setMsgUpload()
        this.setProgress(disable)
    }

    setMsgUpload() {
        this.msgUpload = MESSAGES.processing
        if (!this.disUpload) {
            this.msgUpload = MESSAGES.verify
        }
    }

    afterLoadTable() {
        this.disImportSel = true
        this.disFile = true
        this.disUpload = false
        this.disClear = false
        this.disFilters = false
        this.setProgress(false)
        this.setMsgUpload()
    }

    clear() {
        this.disUpload = true
        this.disAll(false)
        this.fileName = ""
        this.sheetName = ""
        this.jsonObj = []
        this.tableData = []
        this.table.populate([], [])
        this.hideTable = true
        this.closeError()
        this.closeUploadError()
    }

    clearLocked() {
        this.disUpload = true
        this.disAll(false)
        this.closeError()
        if (Array.isArray(this.tableData) && this.tableData.length > 0) {
            this.disImportSel = true
            this.disFile = true
            this.disUpload = false
            this.disClear = false
            this.disFilters = false
            this.setProgress(false)
            this.dropDis = true
            this.setMsgUpload()
        }
    }

    disLocked(msg: string) {
        this.disFile = true
        this.disClear = true
        this.dropDis = true
        this.msgUpload = MESSAGES.locked2
        var msg2 = MESSAGES.locked + " " + msg
        this.processLocalError(this.title, msg2, null, null, null)
    }

    // Intervals Filter
    filtersInit() {
        const saved = this.acctSvc.getCurrentUser()?.filter
        this.fIntervalSelected = this.fIntervals.find(n => n.id === (saved?.intervalId ?? this.intervalId))
        this.fYearSelected = this.fYears.find(n => n.year == (saved?.year ?? this.currentYear ?? new Date().getFullYear()))
        this.intervalChange(false)
    }

    intervalChange(saveSettings = true) {
        if (this.fIntervalSelected?.id == Intervals.Yearly) {
            if (saveSettings) {
                this.saveFilters()
            }
        }
        else {
            this.getFilter()
        }
    }

    loadInterval(fIntervals: FiltersIntervalsData[]) {
        const saved = this.acctSvc.getCurrentUser()?.filter
        switch (Number(this.fIntervalSelected?.id)) {
            case Intervals.Weekly:
                this.fWeeks = fIntervals
                this.fWeekSelected = fIntervals.find(w => w.number === saved?.week) ??
                    fIntervals.find(w => w.id == this.calendarId) ?? fIntervals[0]

                this.weekChange()
                break
            case Intervals.Monthly:
                this.fMonths = fIntervals
                this.fMonthSelected = fIntervals.find(m => m.month === saved?.month) ??
                    fIntervals.find(it => it.id == this.calendarId) ?? fIntervals[0]

                this.monthChange()
                break
            case Intervals.Quarterly:
                this.fQuarters = fIntervals
                this.fQuarterSelected = fIntervals.find(q => q.number === saved?.quarter) ??
                    fIntervals.find(it => it.id == this.calendarId) ?? fIntervals[0]

                this.quarterChange()
                break
            case Intervals.Yearly:
                this.saveFilters()
                break
        }
    }

    weekChange() {
        if (this.fWeekSelected?.locked) {
            this.disLocked("Week " + this.fWeekSelected.number)
        } else {
            this.clearLocked()
            this.saveFilters()
        }
    }

    monthChange() {
        if (this.fMonthSelected.locked) {
            this.disLocked(this.fMonthSelected.month ?? "Unknown")
        } else {
            this.clearLocked()
            this.saveFilters()
        }
    }

    quarterChange() {
        if (this.fQuarterSelected.locked) {
            this.disLocked("Quarter " + this.fQuarterSelected.number)
        } else {
            this.clearLocked()
            this.saveFilters()
        }
    }

    saveFilters() {
        this.acctSvc.saveSettings({
            ...(this.fIntervalSelected && { intervalId: this.fIntervalSelected.id }),
            ...(this.fYearSelected && { year: this.fYearSelected.year }),
            ...(this.fWeekSelected && { week: this.fWeekSelected.number }),
            ...(this.fMonthSelected && { month: this.fMonthSelected.month }),
            ...(this.fQuarterSelected && { quarter: this.fQuarterSelected.number })
        })
    }

    getFilter() {
        // Call Server
        let params = new HttpParams()
            .set("intervalID", this.fIntervalSelected?.id ?? 0)
            .set("year", this.fYearSelected.year)
            .set("isDataImport", true)
        this.api.getFiltersIntervals(params)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: body => {
                    if (body.data) {
                        this.calendarId = body.calendarId
                        this.loadInterval(body.data)
                    }
                },
                error: (err: HttpErrorResponse) => {
                    this.processLocalError("Filters", err.error, null, err.status, null)
                }
            })
    }

    // -----------------------------------------------------------------------------
    // Buttons
    // -----------------------------------------------------------------------------

    clearClick() {
        this.msgUpload = MESSAGES.clear
        this.clear()
    }

    processDialogAlertFromXls(size: any) {
        let dialogOptions = {
            title: this.title,
            message: MESSAGES.fileSize + size,
            alert: true
        }

        this.dialog.open(AppDialog, { data: dialogOptions })
            .afterClosed().subscribe(_ => this.clear())
        return false
    }

    processDialogAlert(title: string, htmlContent: string) {
        this.dialog.open(AppDialog, { data: { title, htmlContent, alert: true } })
            .afterClosed().subscribe(_ => this.clear())
        return false
    }

    // Selection Import Types
    onSelImportChange() {
        if (this.selImportSelected != null) {
            this.showIntervals = this.selImportSelected.id == 1
            if (this.showIntervals) {
                this.filtersInit()
            }
        }

        this.clear()
    }

    onFileSelected(event: Event) {
        const files = (event.currentTarget as HTMLInputElement).files
        if (files != null) {
            this.onFileDropped(files)
        }
    }

    onFileDropped(files: FileList) {
        this.disAll()
        /* wire up file reader */
        //const target: DataTransfer = <DataTransfer>(evt.target);
        //if (target.files.length !== 1) throw new Error('Cannot use multiple files');
        const reader: FileReader = new FileReader()
        reader.onload = (ev: any) => {
            const ab: ArrayBuffer = ev.target.result
            const wb: WorkBook = read(ab)

            if (wb.SheetNames.length > 1) {
                this.sheetNames = Array.from(wb.SheetNames, (it, i) => ({ id: i, name: it }))
                this.showSheetDialog().subscribe(result => {
                    if (result) {
                        this.calculateJson(files[0].name, wb, result)
                    }
                    else {
                        this.clear()
                    }
                })
            }
            else {
                this.disAll()
                this.calculateJson(files[0].name, wb, wb.SheetNames[0])
            }
        }

        reader.readAsArrayBuffer(files[0])
    }

    calculateJson(fileName: string, wb: WorkBook, sheetName: string) {
        this.fileName = ""
        this.sheetName = sheetName  // "Sheet1"
        let ws = wb.Sheets[sheetName]
        this.colNames = utils.sheet_to_json(ws, { header: 1 })[0] as string[]  // ['Region ID', 'MetricID', 'value']
        this.jsonObj = utils.sheet_to_json(ws) as { [hdr: string]: JsonValue }[]
        let colNamesTrim = []

        // Validates for empty or undefined column names
        for (let name of this.colNames) {
            let _ = colNamesTrim.push(name.replace(/\s/, "").toLowerCase())
            if (!name) {
                this.processDialogAlert("Column Validation", "Columns with values cannot be blank or empty.")
                return
            }
        }

        // Validates non matching and required column names
        let dataImport = this.selImport.filter(it => it.id == this.selImportSelected?.id)
        if (dataImport.length > 0) {
            // Validates non matching column names
            for (let name of this.colNames) {
                let headingTitle = name.replace(/\s/, "").toLowerCase()
                let bFound = dataImport[0].heading.some(h => h.title == headingTitle)

                // Could not find name
                if (!bFound) {
                    this.processDialogAlert("Column Validation", `Column '${ headingTitle }' is not part of the import.`)
                    return
                }
            }

            // Validates required column names
            for (let heading of dataImport[0].heading) {
                if (heading == null) {
                    this.processDialogAlert("Column Validation", "Columns are required.")
                    return
                }

                if (heading.required && colNamesTrim.indexOf(heading.title) < 0) {
                    this.processDialogAlert("Column Validation", `Column '${ heading.title }' is required.`)
                    return
                }
            }
        }

        // Success
        this.fileName = fileName
        if (this.loadTable()) {
            this.afterLoadTable()
        }
    }

    processUpload() {
        let dataOut: DataOut = {
            dataImport: this.selImportSelected.id,
            sheet: this.sheetName,
            data: this.jsonObj
        }

        if (dataOut.dataImport == 1) {  // measure data
            var msgCalendar = ""
            var msgInterval = "<strong>Interval:</strong> " + this.fIntervalSelected?.name + LINE1

            switch (Number(this.fIntervalSelected?.id)) {
                case Intervals.Weekly:
                    dataOut.calendarId = this.fWeekSelected.id
                    msgCalendar = "<strong>Year:</strong> " + this.fYearSelected.year + LINE1
                        + "<strong>Week:</strong> " + this.fWeekSelected.number + ": "
                        + formatDate(this.fWeekSelected.startDate ?? 0, "mediumDate", this.locale) + " to "
                        + formatDate(this.fWeekSelected.endDate ?? 0, "mediumDate", this.locale)
                    break
                case Intervals.Monthly:
                    dataOut.calendarId = this.fMonthSelected.id
                    msgCalendar = "<strong>Year:</strong> " + this.fYearSelected.year + LINE1
                        + "<strong>Month:</strong> " + this.fMonthSelected.month
                    break
                case Intervals.Quarterly:
                    dataOut.calendarId = this.fQuarterSelected.id
                    msgCalendar = "<strong>Year:</strong> " + this.fYearSelected.year + LINE1
                        + "<strong>Quarter:</strong> "
                        + formatDate(this.fQuarterSelected.startDate ?? 0, "mediumDate", this.locale) + " to "
                        + formatDate(this.fQuarterSelected.endDate ?? 0, "mediumDate", this.locale)
                    break
                case Intervals.Yearly:
                    dataOut.calendarId = this.fYearSelected.id
                    msgCalendar = "<strong>Year:</strong> " + this.fYearSelected.year
                    break
            }

            msgCalendar = msgInterval + msgCalendar

            var title = "Confirmation Upload"
            var htmlContent = "Are these values correct?" + LINE2 + msgCalendar
            this.dialog.open(AppDialog, { data: { title, htmlContent } })
                .afterClosed().subscribe(result => {
                    if (result) {
                        this.upload(dataOut)
                    }
                })
        }
        else {
            this.upload(dataOut)
        }
    }

    upload(body: DataOut) {
        //vm.dataOut.data = angular.toJson(jsonObj);
        this.msgUpload = MESSAGES.upload

        // Call Server
        this.setProgress(true)
        this.http.post<UploadsBody>(environment.baseUrl + "api/dataimports/upload", body)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: body => {
                    if (body.error == null) {
                        this.processLocalError(this.title, JSON.stringify(body), null, null, null)
                    }
                    else {
                        if (body.error.length > 0) {
                            this.processUploadError(body.error)
                        }
                        else {
                            this.logger.logSuccess(MESSAGES.uploadSuccess)
                            this.setProgress(false)
                            this.clearClick()
                        }
                    }
                },
                error: (err: HttpErrorResponse) => {
                    this.processLocalError(this.title, err.error, null, err.status, null)
                }
            })
    }

    loadTable() {
        try {
            // Validates data
            if (this.jsonObj == null) {
                this.errorMsg = "There is an error with the file."
                return
            }

            this.disAll()
            this.hideTable = false
            this.tableData = []
            // Only for Customers
            if (this.selImportSelected.id == 3) {
                for (let i = 0, len = this.jsonObj.length; i < 100; i++) {
                    if (i < len) {
                        let item = this.jsonObj[i]
                        let obj: { [key: string]: any } = {}
                        for (var k in item) {
                            obj[k] = item[k]
                        }

                        let _ = this.tableData.push(obj)
                    }
                }
            }
            else {
                this.tableData = this.jsonObj
            }

            this.table.populate(this.colNames, this.tableData)
            return true
        }
        catch (err: any) {
            this.errorMsg = `Error ${ err.name } ${ err.message }`
            return false
        }
    }

    identity(_: number, item: { id: number }) {
        return item.id
    }
}
