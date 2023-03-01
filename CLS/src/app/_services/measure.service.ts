import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Measure } from '../_models/measure';

@Injectable({
  providedIn: 'root'
})
export class MeasureService {

  private baseUrl = environment.baseUrl + 'api/measures';
  
  constructor(private http: HttpClient) { }

  // Get Measure from API

  getMeasure(): Observable<Measure[]>{
    //return this.http.get<MeasureData[]>(environment.baseUrl + 'api/measuredata/index');
    return this.http.get<Measure[]>(this.baseUrl + '/index').pipe(
      map((response: Measure[]) => {
        const measureOnService = response
        console.log("Measure On Service : ", measureOnService);
        return measureOnService
      }),
    );
  }


}
