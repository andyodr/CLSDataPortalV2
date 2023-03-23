import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../environments/environment';
import { ErrorModel } from '../_models/error';
import { MeasureDataReceiveObject, MeasureDataResponse } from '../_models/measureData';
import { FilterResponseDto } from "./measure-definition.service"

export type FiltersIntervalsData = {
  error?: ErrorModel
  id: number
  number?: number | null
  startDate?: string
  endDate?: string
  month?: string
  locked?: boolean
}

export type FiltersIntervalsDto = {
  calendarId: number
  data: FiltersIntervalsData[]
}

@Injectable({
  providedIn: 'root'
})
export class MeasureDataService {

  private baseUrl = environment.baseUrl + 'api/measuredata/';

  constructor(private http: HttpClient) { }

  getFilters() {
    return this.http.get<FilterResponseDto>(this.baseUrl + "filter")
  }

  getFiltersIntervals(params: HttpParams) {
    return this.http.get<FiltersIntervalsDto>(environment.baseUrl + "api/filters/intervals", { params })
  }

  // Get Measure Data from API
  //getMeasureData(measureData: MeasureData): Observable<MeasureData[]>{
  getMeasureData1() {
    const calId = 649
    const day = "3/9/2023";
    const hierarchyId = 1;
    const measureTypeId = 1;
    const explanation = 'explanation-value';
    const action = 'action-value';
    //query params
    let params = new HttpParams();
    //params = params.append('CalendarId', measureDataReceiveObject.calendarId!);
    params = params.append('CalendarId', calId);
    params = params.append('Day', day);
    params = params.append('HierarchyId', hierarchyId);
    params = params.append('MeasureTypeId', measureTypeId);
    params = params.append('Explanation', explanation);
    params = params.append('Action', action);
    console.log("Params : ", params);

    //return this.http.get<MeasureDataResponse>(this.baseUrl + '/index?',{params:params});

    return this.http.get<MeasureDataResponse>(this.baseUrl + '/index?', { params: params }).pipe(
      map((response: MeasureDataResponse) => {
        const measureDataOnService = response
        console.log("=====Measure Data On Service======");
        console.log(JSON.stringify(measureDataOnService));
        console.log("=====Measure Data On Service======");
        //response.data = this.dataSeed;
        return measureDataOnService
      }),
    );
  }

  getMeasureData(filtered: MeasureDataReceiveObject) {

    //query params
    let params = new HttpParams();
    //params = params.append('CalendarId', measureDataReceiveObject.calendarId!);
    params = params.append('CalendarId', filtered.calendarId);
    params = params.append('Day', filtered.day);
    params = params.append('HierarchyId', filtered.hierarchyId);
    params = params.append('MeasureTypeId', filtered.measureTypeId);
    params = params.append('Explanation', filtered.explanation);
    params = params.append('Action', filtered.action);
    console.log("Params : ", params);

    //return this.http.get<MeasureDataResponse>(this.baseUrl + '/index?',{params:params});

    return this.http.get<MeasureDataResponse>(this.baseUrl + '/index?', { params: params }).pipe(
      map((response: MeasureDataResponse) => {
        const measureDataOnService = response
        console.log("=====Measure Data On Service======");
        console.log(JSON.stringify(measureDataOnService));
        console.log("=====Measure Data On Service======");
        //response.data = this.dataSeed;
        return measureDataOnService
      }),
    );

  }

  // Get Measure Data from API



}
