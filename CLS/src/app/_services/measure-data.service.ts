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
  //getMeasureData(measureData: MeasureData): Observable<MeasureData[]>{
  getMeasureData(): Observable<MeasureData[]>{
    
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
    

    return this.http.get<MeasureData[]>(this.baseUrl + '/index?',{params:params}).pipe(
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



