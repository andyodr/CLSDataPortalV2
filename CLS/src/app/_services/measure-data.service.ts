import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { ErrorModel } from '../_models/error';
import { FilterResponseDto, MeasureDataDto, MeasureDataPutDto, MeasureDataResponse } from '../_models/measureData';


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
    return this.http.get<FilterResponseDto>(this.baseUrl + "filter").pipe(
      map((response: FilterResponseDto) => {
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
    return this.http.get<MeasureDataResponse>(this.baseUrl + "index/", {params}).pipe(
      map((response: MeasureDataResponse) => {
        console.log("Measure Data Response : ", response)
        return response
      })
    )
  }

  updateMeasureData(body:MeasureDataPutDto): Observable<MeasureDataResponse> {
    return this.http.put<MeasureDataResponse>(this.baseUrl + "Index/", body).pipe(
      map((response: MeasureDataResponse) => {
        console.log("Measure Data Response : ", response)
        return response
      })
    )
  }
      
    // .pipe(
    //   map((response: MeasureDataEditDto) => {
    //     console.log("Measure Data Edit Response : ", response)
    //     return response
    //   })
    // )
  

}
