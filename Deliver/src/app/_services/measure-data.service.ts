import { HttpClient, HttpParams } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { map, Observable } from "rxjs"
import { environment } from "../../environments/environment"
import { FiltersIntervalsDto, MeasureDataApiResponse, MeasureDataFilterResponseDto, MeasureDataPutDto } from "../_models/measureData"

@Injectable({ providedIn: "root" })
export class MeasureDataService {

    private baseUrl = environment.baseUrl + "api/measuredata/"

    constructor(private http: HttpClient) { }

    getFilters(): Observable<MeasureDataFilterResponseDto> {
        return this.http.get<MeasureDataFilterResponseDto>(this.baseUrl + "filter").pipe(
            map((response: MeasureDataFilterResponseDto) => {
                return response
            })
        )
    }

    getFiltersIntervals(params: HttpParams): Observable<FiltersIntervalsDto> {
        return this.http.get<FiltersIntervalsDto>(environment.baseUrl + "api/filters/intervals", { params }).pipe(
            map((response: FiltersIntervalsDto) => {
                return response
            })
        )
    }

    getMeasureDataList(params: HttpParams): Observable<MeasureDataApiResponse> {
        return this.http.get<MeasureDataApiResponse>(this.baseUrl + "Index/", { params }).pipe(
            map((response: MeasureDataApiResponse) => {
                return response
            })
        )
    }

    updateMeasureData(body: MeasureDataPutDto): Observable<MeasureDataApiResponse> {
        return this.http.put<MeasureDataApiResponse>(this.baseUrl + "Index/", body).pipe(
            map((response: MeasureDataApiResponse) => {
                return response
            })
        )
    }
}
