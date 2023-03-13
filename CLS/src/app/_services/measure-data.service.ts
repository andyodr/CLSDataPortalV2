import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Data, MeasureDataResponse } from '../_models/measureData';

@Injectable({
  providedIn: 'root'
})
export class MeasureDataService {

  private baseUrl = environment.baseUrl + 'api/measuredata/';

  dataSeed: Data[] = [
    {
      "id":2260187,
      "name":"CRM Order Entry Errors",
      "value":206,
      "explanation":null,
      "action":null,
      "target":null,
      "targetCount":null,
      "targetId":null,
      "unitId":2,
      "units":"#",
      "yellow":null,
      "expression":null,
      "evaluated":"",
      "calculated":true,
      "description":"Number of Order Entry errors documented",
      "updated":{
          "by":"System",
          "longDt":"3/8/2023 4:29:07 PM",
          "shortDt":"4 days 3 hours"
        }
      },
      {
      "id":2260129,
      "name":"CRM Order Fill Error",
      "value":301.7,
      "explanation":null,
      "action":null,
      "target":null,
      "targetCount":null,
      "targetId":null,
      "unitId":2,
      "units":"#",
      "yellow":null,
      "expression":null,
      "evaluated":"",
      "calculated":true,
      "description":"Number of Order Fill errors documented",
      "updated":{
          "by":"System",
          "longDt":"3/8/2023 5:14:08 PM",
          "shortDt":"4 days 3 hours"
         }
      },
      {
      "id":2259955,
      "name":"CS ERP Orders Entered",
      "value":1314508,
      "explanation":null,
      "action":null,
      "target":null,
      "targetCount":null,
      "targetId":null,
      "unitId":2,
      "units":"#",
      "yellow":null,
      "expression":null,
      "evaluated":"",
      "calculated":true,
      "description":"The total number of sales document transactions that could result in outbound orders entered by Customer Service",
      "updated":{
          "by":"System",
          "longDt":"3/8/2023 4:29:07 PM",
          "shortDt":"4 days 3 hours"
          }
        }
  ];
  
  constructor(private http: HttpClient) { }

  // Get Measure Data from API
  //getMeasureData(measureData: MeasureData): Observable<MeasureData[]>{
  getMeasureData() {
    
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
    

    //return this.http.get<MeasureDataResponse>(this.baseUrl + '/index?',{params:params});

    return this.http.get<MeasureDataResponse>(this.baseUrl + '/index?',{params:params}).pipe(
      map((response: MeasureDataResponse) => {
        const measureDataOnService = response
        console.log("=====Measure Data On Service======");
        console.log(JSON.stringify(measureDataOnService));
        console.log("=====Measure Data On Service======");
        response.data = this.dataSeed;
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



