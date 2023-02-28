import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Target } from '../_models/target';

@Injectable({
  providedIn: 'root'
})
export class TargetService {

  private baseUrl = environment.baseUrl + 'api/targets';
  
  constructor(private http: HttpClient) { 
    console.log("Target Service Constructor");
  }

  //Get Target from API 
  getTarget(): Observable<Target[]>{
    return this.http.get<Target[]>(this.baseUrl + '/index').pipe(
      map((response: Target[]) => {
        const targetOnService = response
        console.log("Target On Service : ", targetOnService);
        return targetOnService
      }),
    );
  }
  
}
