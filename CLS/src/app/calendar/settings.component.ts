import { Component, OnInit } from "@angular/core"
import { TimeSpan } from "../lib/time/time-input.component"
import { CalendarSettingsService, CalendarLock, UserSettingDto } from "../_services/settings.service"

@Component({
    selector: "app-settings",
    templateUrl: "./settings.component.html",
    styleUrls: ["./settings.component.scss"]
})
export class CalendarSettingsComponent implements OnInit {
    disabledAll = false
    years: number[] = []
    yearSelected: number = 0
    active: boolean = false
    calcSchedule: TimeSpan = { hours: 99, minutes: 59, seconds: 59 }
    lastCalculatedOn: string = "3/25/2023 2:24:44 AM"
    locks: CalendarLock[] = []
    locksColumns = ["month", "startDate", "endDate", "locked"]
    users: UserSettingDto[] = []
    usersColumns = ["userName", ...[...Array(12).keys()].map(i => "lo"+(1+i))]
    constructor(private api: CalendarSettingsService) { }

    ngOnInit(): void {
        this.api.getSettings().subscribe({
            next: dto => {
                this.years = dto.years ?? []
                this.yearSelected = dto.year
                this.active = dto.active ?? false
                this.calcSchedule = new TimeSpan(dto.calculateHH ?? 99, dto.calculateMM ?? 59, dto.calculateSS ?? 59)
                this.lastCalculatedOn = dto.lastCalculatedOn
                this.locks = dto.locked ?? []
                this.users = dto.users
                this.loadTable()
            }
        })
    }

    /** clicking this button runs a job that execs 2 password-protected SSIS packages */
    transfer() {
    }

    intervalChange() {
    }

    save() {
    }

    loadTable() {
    }

    identity(_: number, item: { id: number }) {
        return item.id
    }
}
