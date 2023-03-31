import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { TargetFilter, TargetApiParams, TargetApiResponse, TargetDto, TargetFilterResponseDto, TargetPutDto } from '../_models/target';

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

    private baseUrl = environment.baseUrl + 'api/targets';

    constructor(private http: HttpClient) { }

    getTargetFilter(): Observable<TargetFilterResponseDto> {
        return this.http.get<TargetFilterResponseDto>(this.baseUrl + "/filter")
    }


    getTarget(): Observable<TargetApiResponse> {
        return this.http.get<TargetApiResponse>(this.baseUrl + '/index?hierarchyId=1&measureTypeId=1')
    }

    getTargetList(params: HttpParams): Observable<TargetApiResponse> {
        return this.http.get<TargetApiResponse>(this.baseUrl + '/index?hierarchyId=1&measureTypeId=1').pipe(
            map((response: TargetApiResponse) => {
                const targetOnService = response
                console.log("Target On Service : ", targetOnService);
                return targetOnService
            }),
        );
    }

    getTarget1() {
        return this.http.get<TargetApiResponse>(environment.baseUrl + 'api/targets/index?hierarchyId=1&measureTypeId=1').pipe(
            map((response: TargetApiResponse) => {
                const targetOnService = response
                console.log("Target On Service : ", targetOnService);
                return targetOnService
            }),
        );
    }

    getTarget2(filtered: TargetApiParams): Observable<TargetApiResponse> {
        //request params
        let params = new HttpParams();
        params = params.append('hierarchyId', filtered.hierarchyId);
        params = params.append('measureTypeId', filtered.measureTypeId);
        console.log("Params : ", params);
        return this.http.get<TargetApiResponse>(this.baseUrl + '/index', { params: params }).pipe(
            map((response: TargetApiResponse) => {
                const targetOnService = response
                console.log("Target On Service : ", targetOnService);
                return targetOnService
            }
            )
        );
    }

    applyTargetToChildren(dto: TargetDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(`${ this.baseUrl }/targets/Index/`, dto)
    }

    updateTarget(body: TargetPutDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(this.baseUrl + "/Index", body).pipe(
            map((response: TargetApiResponse) => {
                console.log("Target Response on Service : ", response);
                return response
            }),
        );
    }

}
