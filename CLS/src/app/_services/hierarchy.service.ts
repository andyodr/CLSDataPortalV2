import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Hierarchy, HierarchyAdd, HierarchyApiResult } from '../_models/regionhierarchy';

@Injectable({
    providedIn: 'root'
})
export class HierarchyService {

    private baseUrl = environment.baseUrl + 'api/hierarchy'

    constructor(private http: HttpClient) { }

    getHierarchy(): Observable<HierarchyApiResult> {
        return this.http.get<HierarchyApiResult>(this.baseUrl + "/index")
    }

    addHierarchy(add: HierarchyAdd): Observable<HierarchyApiResult> {
        return this.http.post<HierarchyApiResult>(this.baseUrl + "/index", add)
    }

    updateHierarchy(update: Hierarchy): Observable<HierarchyApiResult> {
        return this.http.put<HierarchyApiResult>(this.baseUrl + "/index", update)
    }
}
