import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { MeasureDefinition } from '../_models/measureDefinition';

@Injectable({
  providedIn: 'root'
})
export class MeasureDefinitionService {

  private baseUrl = environment.baseUrl + 'api/measureDefinition';
  
  constructor(private http: HttpClient) { }

  // Get Measure Definition from API
  getMeasureDefinition(): Observable<MeasureDefinition[]>{
    //return this.http.get<MeasureData[]>(environment.baseUrl + 'api/measuredata/index');
    return this.http.get<MeasureDefinition[]>(this.baseUrl + '/index').pipe(
      map((response: MeasureDefinition[]) => {
        const measureDefinitionOnService = response
        console.log("Measure Definition On Service : ", measureDefinitionOnService);
        return measureDefinitionOnService
      }),
    );
  }
}
