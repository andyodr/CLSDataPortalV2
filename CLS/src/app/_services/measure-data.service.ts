import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { MeasureData } from '../_models/measureData';

@Injectable({
  providedIn: 'root'
})
export class MeasureDataService {

  private baseUrl = environment.baseUrl + 'api/measuredata/';
  
  constructor(private http: HttpClient) { }

  // Get Measure Data from API
  getMeasureData(): Observable<MeasureData[]>{
    
    const callId = 711
    const date = Date.now();
    console.log("Date : ", date);
    const explanation = null;
    //query params
    let params = new HttpParams();
    params = params.append('CalendarId', callId);
    params = params.append('Day', date);
    //params = params.append('Explanation', explanation);
    //params = params.append('Action', null);
    console.log("Params : ", params);
    
    
    return this.http.get<MeasureData[]>(this.baseUrl + '/Index',{params:params}).pipe(
      map((response: MeasureData[]) => {
        const measureDataOnService = response
        console.log("Measure Data On Service : ", measureDataOnService);
        return measureDataOnService
      }),
    );
  }

  



  /*getUsers(): Observable<User[]>{
    //return this.http.get<User[]>(environment.baseUrl + 'api/users/index');
    return this.http.get<User[]>(this.baseUrl + '/index').pipe(
      map((response: User[]) => {
        const usersOnService = response
        console.log("Users On Service : ", usersOnService);
        return usersOnService
      }),
    );
  }*/



}



