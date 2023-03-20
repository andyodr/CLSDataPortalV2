import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Filter, TargetApiParams, TargetApiResponse } from '../_models/target';

@Injectable({
  providedIn: 'root'
})
export class TargetService {

  private baseUrl = environment.baseUrl + 'api/targets';
  
  constructor(private http: HttpClient) {}

  //Get Target from API 
  getTarget(): Observable<TargetApiResponse>{
    return this.http.get<TargetApiResponse>(this.baseUrl + '/index?hierarchyId=1&measureTypeId=1')
  }
  
  //Get Target from API 
  getTarget1(){
    return this.http.get<TargetApiResponse>(environment.baseUrl + 'api/targets/index?hierarchyId=1&measureTypeId=1').pipe(
      map((response: TargetApiResponse) => {
        const targetOnService = response
        console.log("Target On Service : ", targetOnService);
        return targetOnService
      }),
    );
  }

  getTarget2(filtered: TargetApiParams): Observable<TargetApiResponse>{
        //request params
        let params = new HttpParams();
        params = params.append('hierarchyId', filtered.hierarchyId);
        params = params.append('measureTypeId', filtered.measureTypeId);
        console.log("Params : ", params);
    return this.http.get<TargetApiResponse>(this.baseUrl + '/index', {params: params}).pipe(
      map((response: TargetApiResponse) => {
        const targetOnService = response
        console.log("Target On Service : ", targetOnService);
        return targetOnService
      }
    ));
  }


}
