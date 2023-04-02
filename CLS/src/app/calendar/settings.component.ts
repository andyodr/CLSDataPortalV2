import { AfterViewInit, Component, OnInit, ViewChild } from "@angular/core"
import { MatSort } from "@angular/material/sort"
import { MatTableDataSource } from "@angular/material/table"
import { TimeSpan } from "../lib/time/time-input.component"
import { LoggerService } from "../_services/logger.service"
import { CalendarSettingsService, CalendarLock, UserSettingDto } from "../_services/settings.service"

@Component({
    selector: "app-settings",
    templateUrl: "./settings.component.html",
    styleUrls: ["./settings.component.scss"]
})
export class CalendarSettingsComponent implements OnInit, AfterViewInit {
    disabledAll = false
    hideProgress = true
    years: number[] = []
    yearSelected: number = 0
    active: boolean = false
    calcSchedule: TimeSpan = { hours: 99, minutes: 59, seconds: 59 }
    lastCalculatedOn: string = "3/25/2023 2:24:44 AM"
    locks: CalendarLock[] = []
    locksColumns = ["month", "startDate", "endDate", "locked"]
    users = new MatTableDataSource<UserSettingDto>()
    @ViewChild(MatSort) sort!: MatSort
    usersColumns = ["userName", ...[...Array(12).keys()].map(i => "lo" + (1 + i))]
    constructor(private api: CalendarSettingsService, private logger: LoggerService) { }

    ngOnInit(): void {
        this.intervalChange()
    }

    ngAfterViewInit() {
        this.users.sort = this.sort
    }

    /** this button runs a job that execs 2 password-protected SSIS packages */
    transfer() {
    }

    intervalChange(year?: number) {
        this.hideProgress = false
        this.api.getSettings(year).subscribe({
            next: dto => {
                this.years = dto.years ?? []
                this.yearSelected = dto.year
                this.active = dto.active ?? false
                this.calcSchedule = new TimeSpan(dto.calculateHH ?? 0, dto.calculateMM ?? 0, dto.calculateSS ?? 0)
                this.lastCalculatedOn = dto.lastCalculatedOn
                this.locks = (dto.locked ?? []).map(c => ({
                    id: c.id,
                    month: c.month ?? "",
                    startDate: c.startDate == null ? "-" : new Intl.DateTimeFormat().format(new Date(c.startDate)),
                    endDate: c.endDate == null ? "-" : new Intl.DateTimeFormat().format(new Date(c.endDate)),
                    locked: c.locked ?? false
                }))
                this.users.data = dto.users
                this.hideProgress = true
            }
        })
    }

    save() {
        this.hideProgress = false
        this.api.updateSettings({
            year: this.yearSelected,
            calculateHH: this.calcSchedule.hours,
            calculateMM: this.calcSchedule.minutes,
            calculateSS: this.calcSchedule.seconds,
            active: this.active,
            locked: this.locks
        }).subscribe({
            next: dto => {
                this.active = dto.active ?? false
                this.calcSchedule.hours = dto.calculateHH ?? 0
                this.calcSchedule.minutes = dto.calculateMM ?? 0
                this.calcSchedule.seconds = dto.calculateSS ?? 0
                this.lastCalculatedOn = dto.lastCalculatedOn
                this.locks = (dto.locked ?? []).map(c => ({
                    id: c.id,
                    month: c.month ?? "",
                    startDate: c.startDate == null ? "-" : new Intl.DateTimeFormat().format(new Date(c.startDate)),
                    endDate: c.endDate == null ? "-" : new Intl.DateTimeFormat().format(new Date(c.endDate)),
                    locked: c.locked ?? false
                }))
                this.logger.logSuccess("Main and monthly lock data updated")
                this.hideProgress = true
            }
        })
    }

    applyFilter(event: Event) {
        const filterValue = (event.currentTarget as HTMLInputElement).value
        this.users.filter = filterValue.trim().toLowerCase()
    }

    lockChange(user: UserSettingDto) {
        this.hideProgress = false
        this.api.updateUser({ year: this.yearSelected, user: user }).subscribe(r => {
            this.hideProgress = true
        })
    }

    identity(_: number, item: { id: number }) {
        return item.id
    }
}
