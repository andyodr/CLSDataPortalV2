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
  getMeasureData(measureDataReceiveObject: MeasureData): Observable<MeasureData[]>{
    
    const callId = 711
    const day = 3;
    // const hierarchyId = 4;
    // const measureTypeId = 2;
    const explanation = 'explanation-value';
    const action = 'action-value';
    //query params
    let params = new HttpParams();
    params = params.append('CalendarId', measureDataReceiveObject.calendarId!);
    params = params.append('Day', day);
    // params = params.append('Day', day);
    // params = params.append('Day', day);
    params = params.append('Explanation', explanation);
    params = params.append('Action', action);
    console.log("Params : ", params);
    

    return this.http.get<MeasureData[]>(this.baseUrl + '/Index',{params:params}).pipe(
      map((response: MeasureData[]) => {
        const measureDataOnService = response
        console.log("Measure Data On Service : ", measureDataOnService);
        return measureDataOnService
      }),
    );
  }

  getData(){
    throw new Error('Method not implemented.');

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



