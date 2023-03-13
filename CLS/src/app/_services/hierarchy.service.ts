import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { HierarchyApiResult } from '../_models/regionhierarchy';

@Injectable({
    providedIn: 'root'
})
export class HierarchyService {

    private baseUrl = environment.baseUrl + 'api/hierarchy'

    constructor(private http: HttpClient) { }

    // Get Hierarchy from API
    getHierarchy(): Observable<HierarchyApiResult> {
        return this.http.get<HierarchyApiResult>(this.baseUrl + "/index")
    }
}
