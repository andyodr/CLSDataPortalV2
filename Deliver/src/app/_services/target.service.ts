import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { TargetFilter, TargetApiResponse, TargetPutDto } from "../_models/target"

  @Injectable({
    providedIn: 'root'
})
export class TargetService {

    private baseUrl = environment.baseUrl + "api/targets"

    constructor(private http: HttpClient) { }

    getTargetFilter(): Observable<TargetFilter> {
        return this.http.get<TargetFilter>(this.baseUrl + "/filter").pipe(
            map((response: TargetFilter) => {
                return response
            })
        )
    }

    getTargetList(params: HttpParams): Observable<TargetApiResponse> {
        return this.http.get<TargetApiResponse>(this.baseUrl , {params}).pipe(
            map((response: TargetApiResponse) => {
                return response
            }),
        );
    }

    updateTarget(body: TargetPutDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(this.baseUrl, body).pipe(
            map((response: TargetApiResponse) => {
                return response
            }),
        );
    }

    updateTarget2(id: number, body: TargetPutDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(`{this.baseUrl}/${ id }`, body ).pipe(
            map((response: TargetApiResponse) => {
                return response
            }),
        );
    }

    applyTargetToChildren(body: TargetPutDto): Observable<TargetApiResponse> {
        return this.http.put<TargetApiResponse>(this.baseUrl, body ).pipe(
            map((response: TargetApiResponse) => {
                return response
            }),
        );
    }
}
