import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core"
import { Observable } from "rxjs";
import { environment } from "../environments/environment";
import { ErrorModel } from "../_models/error";

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

@Injectable({
  providedIn: "root"
})
export class CalendarSettingsService {

  constructor(private http: HttpClient) { }

  getSettings(): Observable<SettingsResponseDto> {
    return this.http.get<SettingsResponseDto>(environment.baseUrl + "api/settings/index")
  }
}
