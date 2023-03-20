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
    interval?: string
    intervalId: number
    varName: string
    description?: string
    expression?: string
    precision: number
    priority: number
    fieldNumber: number
    unitId: number
    units?: string
    calculated?: boolean
    daily?: boolean
    weekly?: boolean
    monthly?: boolean
    quarterly?: boolean
    yearly?: boolean
    aggFunction?: string
    aggFunctionId?: number
}

export type Units = { id: number, name: string, shortName: string }

export type MeasureDefinitionEditDto = {
    units: Units[]
    intervals: { id: number, name: string }[]
    measureTypes: MeasureType[]
    aggFunctions: { id: number, name: string }[]
    data: MeasureDefinition[]
}

@Injectable({
    providedIn: "root"
})
export class MeasureDefinitionService {

    private baseUrl = environment.baseUrl + "api/measuredefinition";

    constructor(private http: HttpClient) { }

    getMeasureDefinition(measureTypeId: number): Observable<{ data: MeasureDefinition[] }> {
        return this.http.get<{ data: MeasureDefinition[] }>(`${ this.baseUrl }/index/${ measureTypeId }`)
    }

    getMeasureDefinitionFilter(): Observable<MeasureDefinitionFilter> {
        return this.http.get<MeasureDefinitionFilter>(this.baseUrl + "/filter")
    }

    getMeasureDefinitionEdit(measureDefinitionId?: number): Observable<MeasureDefinitionEditDto> {
        if (measureDefinitionId == null) {
            return this.http.get<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/add`)
        }
        else {
            return this.http.get<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/edit/${ measureDefinitionId }`)
        }
    }

    updateMeasureDefinition(id: number, dto: MeasureDefinition): Observable<MeasureDefinitionEditDto> {
        return this.http
        .put<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/edit/${ id }`, dto)
    }

    addMeasureDefinition(dto: MeasureDefinition): Observable<MeasureDefinitionEditDto> {
        return this.http
        .post<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/add`, dto)
    }
}
