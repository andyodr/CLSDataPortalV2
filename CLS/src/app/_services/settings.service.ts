import { HttpClient, HttpErrorResponse } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { Router } from "@angular/router"
import { catchError, Observable, throwError } from "rxjs"
import { environment } from "../environments/environment"
import { ErrorModel } from "../_models/error"

export type CalendarLock = {
    id: number
    month?: string
    startDate?: string
    endDate?: string
    locked?: boolean
}

export type UserSettingDto = {
    id: number
    userName: string
    locks?: { lo: boolean }[]
}

export type SettingsResponseDto = {
    error?: ErrorModel
    years?: number[]
    year: number
    calculateHH?: number
    calculateMM?: number
    calculateSS?: number
    active?: boolean
    lastCalculatedOn: string
    locked?: CalendarLock[]
    users: UserSettingDto[]
}

export type SettingsRequestDto = {
    year: number
    calculateHH: number
    calculateMM: number
    calculateSS: number
    active: boolean
    locked: CalendarLock[]
}

export type UpdateUserSettingDto = {
    year: number
    user: UserSettingDto
}

@Injectable({
  providedIn: "root"
})
export class CalendarSettingsService {

  constructor(private http: HttpClient, private router: Router) { }

  getSettings(year?: number): Observable<SettingsResponseDto> {
    const url = environment.baseUrl + "api/settings/index"
    return this.http.get<SettingsResponseDto>(year ? `${url}/${year}` : url)
  }

  updateSettings(body: SettingsRequestDto) {
    return this.http.put<SettingsResponseDto>(environment.baseUrl + "api/settings/index", body)
  }

  updateUser(body: UpdateUserSettingDto) {
    return this.http.put<SettingsResponseDto>(environment.baseUrl + "api/settings/users", body)
  }
}
