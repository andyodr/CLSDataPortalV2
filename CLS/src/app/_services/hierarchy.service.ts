import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Hierarchy } from '../_models/hierarchy';

@Injectable({
  providedIn: 'root'
})
export class HierarchyService {

  private baseUrl = environment.baseUrl + 'api/hierarchy';
  
  constructor(private http: HttpClient) { }

  // Get Hierarchy from API
  getHierarchy(): Observable<Hierarchy[]>{    
    return this.http.get<Hierarchy[]>(this.baseUrl + '/index').pipe(
      map((response: Hierarchy[]) => {
        const hierarchyOnService = response
        console.log("Hierarchy On Service : ", hierarchyOnService);
        return hierarchyOnService
      }),
    );
  }

}
