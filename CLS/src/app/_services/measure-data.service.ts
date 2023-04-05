import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { ErrorModel } from '../_models/error';
import {FiltersIntervalsDto, MeasureDataApiResponse, MeasureDataDto, MeasureDataFilterResponseDto, MeasureDataPutDto } from '../_models/measureData';




@Injectable({
  providedIn: 'root'
})
export class MeasureDataService {

  private baseUrl = environment.baseUrl + 'api/measuredata/';

  constructor(private http: HttpClient) { }

  getFilters() {
    return this.http.get<MeasureDataFilterResponseDto>(this.baseUrl + "filter").pipe(
      map((response: MeasureDataFilterResponseDto) => {
        console.log("Filter Response : ", response)
        return response
      })
    )
  }

  getFiltersIntervals(params: HttpParams) {
    return this.http.get<FiltersIntervalsDto>(environment.baseUrl + "api/filters/intervals", { params }).pipe(
      map((response: FiltersIntervalsDto) => {
        console.log("Filters Intervals Response : ", response)
        return response
      })
    )
  }

  getMeasureDataList(params: HttpParams) {
    return this.http.get<MeasureDataApiResponse>(this.baseUrl + "Index/", {params}).pipe(
      map((response: MeasureDataApiResponse) => {
        console.log("Measure Data Response : ", response)
        return response
      })
    )
  }

  updateMeasureData(body:MeasureDataPutDto): Observable<MeasureDataApiResponse> {
    return this.http.put<MeasureDataApiResponse>(this.baseUrl + "Index/", body).pipe(
      map((response: MeasureDataApiResponse) => {
        console.log("Measure Data Response : ", response)
        return response
      })
    )
  }

}
