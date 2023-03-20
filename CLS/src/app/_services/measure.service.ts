import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { MeasureApiParams, MeasureApiResponse } from '../_models/measure';
//import { Measure } from '../_models/measure';

@Injectable({
  providedIn: 'root'
})
export class MeasureService {

  private baseUrl = environment.baseUrl + 'api/measures';
  
  constructor(private http: HttpClient) { }

  // Get Measure from API

  // getMeasure(): Observable<Measure[]>{
  //   //return this.http.get<MeasureData[]>(environment.baseUrl + 'api/measuredata/index');
  //   return this.http.get<Measure[]>(this.baseUrl + '/index').pipe(
  //     map((response: Measure[]) => {
  //       const measureOnService = response
  //       console.log("Measure On Service : ", measureOnService);
  //       return measureOnService
  //     }),
  //   );
  // }

  //Get Measures from API 
  getMeasure(): Observable<MeasureApiResponse>{
    return this.http.get<MeasureApiResponse>(this.baseUrl + '/index?hierarchyId=1&measureTypeId=1')
  }
  
  //Get Target from API 
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

}
