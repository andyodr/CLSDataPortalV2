import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { TargetFilter, TargetApiResponse, TargetPutDto } from '../_models/target';

/*export type MeasureType = {
  id: number,
  name: string,
  description?: string
}

export type TargetFilter = {
  measureTypes: MeasureType[]
  filter: {
      hierarchyId?: number
      measureTypeId?: number
      intervalId?: number
      calendarId?: number
      year?: number
  }
}

export interface Target {
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

export type TargetEditDto = {
  units: Units[]
  intervals: { id: number, name: string }[]
  measureTypes: MeasureType[]
  aggFunctions: { id: number, name: string }[]
  data: Target[]
}*/


@Injectable({
    providedIn: 'root'
})
export class TargetService {

    private baseUrl = environment.baseUrl + 'api/targets/';

    constructor(private http: HttpClient) { }

    getTargetFilter(): Observable<TargetFilter> {
        return this.http.get<TargetFilter>(this.baseUrl + "filter")
    }


    getTargetList(params: HttpParams): Observable<TargetApiResponse> {
        return this.http.get<TargetApiResponse>(this.baseUrl + "Index/" , {params}).pipe(
            map((response: TargetApiResponse) => {
                console.log("Target List Response On Service : ", response);
                return response
            }),
        );
    }

    updateTarget(body: TargetPutDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(this.baseUrl + "Index/", body).pipe(
            map((response: TargetApiResponse) => {
                console.log("Update Target Response on Service : ", response);
                return response
            }),
        );
    }

    updateTarget2(id: number, body: TargetPutDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(this.baseUrl + `/Index/${ id }`, body ).pipe(
            map((response: TargetApiResponse) => {
                console.log("Target Response on Service : ", response);
                return response
            }),
        );
    }

    applyTargetToChildren(body: TargetPutDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(this.baseUrl + "Index/", body ).pipe(
            map((response: TargetApiResponse) => {
                console.log("Target Response on Service Apply to Children : ", response);
                return response
            }),
        );
    }

}
