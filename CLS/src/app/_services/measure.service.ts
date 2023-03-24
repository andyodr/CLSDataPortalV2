import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { MeasureApiParams, MeasureApiResponse, MeasureFilter } from '../_models/measure';
//import { Measure } from '../_models/measure';


// export type MeasureType = {
//   id: number,
//   name: string,
//   description?: string
// }

// export type MeasureDefinitionFilter = {
//   measureTypes: MeasureType[]
//   filter: {
//       hierarchyId?: number
//       measureTypeId?: number
//       intervalId?: number
//       calendarId?: number
//       year?: number
//   }
// }

// export interface MeasureDefinition {
//   id?: number
//   name: string
//   measureTypeId: number
//   interval?: string
//   intervalId: number
//   varName: string
//   description?: string
//   expression?: string
//   precision: number
//   priority: number
//   fieldNumber: number
//   unitId: number
//   units?: string
//   calculated?: boolean
//   daily?: boolean
//   weekly?: boolean
//   monthly?: boolean
//   quarterly?: boolean
//   yearly?: boolean
//   aggFunction?: string
//   aggFunctionId?: number
// }

// export type Units = { id: number, name: string, shortName: string }

// export type MeasureDefinitionEditDto = {
//   units: Units[]
//   intervals: { id: number, name: string }[]
//   measureTypes: MeasureType[]
//   aggFunctions: { id: number, name: string }[]
//   data: MeasureDefinition[]
// }


@Injectable({
  providedIn: 'root'
})
export class MeasureService {

  private baseUrl = environment.baseUrl + 'api/measures';
  
  constructor(private http: HttpClient) { }

  //Get Measures from API 
    getMeasure(): Observable<MeasureApiResponse>{
      return this.http.get<MeasureApiResponse>(this.baseUrl + '/index?hierarchyId=1&measureTypeId=1')
    }

    getMeasure1(){
      return this.http.get<MeasureApiResponse>(environment.baseUrl + 'api/targets/index?hierarchyId=1&measureTypeId=1').pipe(
        map((response: MeasureApiResponse) => {
          const targetOnService = response
          console.log("Target On Service : ", targetOnService);
          return targetOnService
        }),
      );
    }

    getMeasure2(filtered: MeasureApiParams): Observable<MeasureApiResponse>{
          //request params
          let params = new HttpParams();
          params = params.append('hierarchyId', filtered.hierarchyId);
          params = params.append('measureTypeId', filtered.measureTypeId);
          console.log("Params : ", params);
      return this.http.get<MeasureApiResponse>(this.baseUrl + '/index', {params: params}).pipe(
        map((response: MeasureApiResponse) => {
          const targetOnService = response
          console.log("Measure On Service : ", targetOnService);
          return targetOnService
        }
      ));
    }


    getMeasureFilter(): Observable<MeasureFilter> {
        return this.http.get<MeasureFilter>(this.baseUrl + "/filter"). pipe(
          map((response: MeasureFilter) => {
              const measureFilter = response
              console.log("Measure Filter on Service: ", measureFilter);
              return measureFilter
          }
        ));
    }

    // getMeasureDefinitionEdit(measureDefinitionId?: number): Observable<MeasureDefinitionEditDto> {
    //     if (measureDefinitionId == null) {
    //         return this.http.get<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/add`)
    //     }
    //     else {
    //         return this.http.get<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/edit/${ measureDefinitionId }`)
    //     }
    // }

    // updateMeasureDefinition(id: number, dto: MeasureDefinition): Observable<MeasureDefinitionEditDto> {
    //     return this.http
    //     .put<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/edit/${ id }`, dto)
    // }

    // addMeasureDefinition(dto: MeasureDefinition): Observable<MeasureDefinitionEditDto> {
    //     return this.http
    //     .post<MeasureDefinitionEditDto>(`${ this.baseUrl }/measure/add`, dto)
    // }

}
