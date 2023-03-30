import { HttpClient, HttpParams } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { Observable } from "rxjs"
import { environment } from "../environments/environment"
import { MeasureType, Filter } from "../_services/measure-definition.service"
import { RegionFilter } from "./hierarchy.service"

export interface MeasureApiParams {
    intervalId?: number
    calendarId?: number
    year?: number
    measureTypeId: number
    hierarchyId: number
}

export interface RegionActiveCalculatedDto {
    id: number
    active: boolean
    expression: boolean
    rollup: boolean
}

export interface MeasureTypeRegionsDto {
    id: number
    name: string
    owner: string
    hierarchy: RegionActiveCalculatedDto[]
}

export interface MeasureApiResponse {
    error: any
    hierarchy: string[]
    allow: boolean
    data: MeasureTypeRegionsDto[]
}

export type MeasureFilter = {
    measureTypes: MeasureType[]
    hierarchy: RegionFilter[]
    intervals: any
    years: any
    error: any
    filter: Filter
    currentCalendarIds: any
    measures: MeasureApiResponse
}

export type MeasureDefinitionPutDto = {
    measureDefinitionId: number
    hierarchy: RegionActiveCalculatedDto[]
}

@Injectable({
    providedIn: "root"
})
export class MeasureService {

    private baseUrl = environment.baseUrl + "api/measures";

    constructor(private http: HttpClient) { }

    getMeasures(filtered: MeasureApiParams): Observable<MeasureApiResponse> {
        let params = new HttpParams()
        params = params.append("hierarchyId", filtered.hierarchyId)
        params = params.append("measureTypeId", filtered.measureTypeId)
        return this.http.get<MeasureApiResponse>(this.baseUrl + "/index", { params: params })
    }

    getMeasureFilter(): Observable<MeasureFilter> {
        return this.http.get<MeasureFilter>(this.baseUrl + "/filter")
    }

    updateMeasures(body: MeasureDefinitionPutDto): Observable<MeasureApiResponse> {
        return this.http.put<MeasureApiResponse>(this.baseUrl + "/index", body)
    }
}
