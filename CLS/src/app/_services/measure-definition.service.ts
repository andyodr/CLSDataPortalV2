import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { Observable } from "rxjs"
import { environment } from "../environments/environment"

export type MeasureType = {
    id: number,
    name: string,
    description?: string
}

export type MeasureDefinitionFilter = {
    measureTypes: MeasureType[]
    filter: {
        hierarchyId?: number
        measureTypeId?: number
        intervalId?: number
        calendarId?: number
        year?: number
    }
}

export interface MeasureDefinition {
    id?: number
    name: string
    measureTypeId: number
    interval: string
    intervalId: number
    varName: string
    description?: string
    expression?: string
    precision: number
    priority: number
    fieldNumber: number
    unitId: number
    units: string
    calculated?: boolean
    daily?: boolean
    weekly?: boolean
    monthly?: boolean
    quarterly?: boolean
    yearly?: boolean
    aggFunction: string
    aggFunctionId?: number
}

@Injectable({
    providedIn: "root"
})
export class MeasureDefinitionService {

    private baseUrl = environment.baseUrl + "api/measuredefinition";

    constructor(private http: HttpClient) { }

    getMeasureDefinition(measureTypeId: number): Observable<{ data: MeasureDefinition[] }> {
        return this.http.get<{ data: MeasureDefinition[] }>(`${this.baseUrl}/index/${measureTypeId}`)
    }

    getMeasureDefinitionFilter(): Observable<MeasureDefinitionFilter> {
        return this.http.get<MeasureDefinitionFilter>(this.baseUrl + "/filter")
    }
}
