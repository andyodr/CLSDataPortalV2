import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { Observable } from "rxjs"
import { environment } from "../../environments/environment"
import { ErrorModel } from "../_models/error"

export type RegionFilter = {
    hierarchy: string
    id: number
    count?: number
    sub: RegionFilter[]
    found?: boolean
    error: ErrorModel
}

export interface HierarchyAdd {
    levelId: number
    name: string
    parentId: number | null
    active?: boolean
    remove?: boolean
}

export interface Hierarchy extends HierarchyAdd {
    id: number
    level?: string
    parentName?: string
}

export type HierarchyApiResult = {
    data: Hierarchy[]
    hierarchy: RegionFilter[]
    regionId: number
    levels: { id: number, name: string }[]
    error: ErrorModel
}

export class RegionFlatNode {
    hierarchy!: string
    level!: number
    expandable!: boolean
}

@Injectable({
    providedIn: "root"
})
export class HierarchyService {

    private baseUrl = environment.baseUrl + "api/hierarchy"

    constructor(private http: HttpClient) { }

    getHierarchy(): Observable<HierarchyApiResult> {
        return this.http.get<HierarchyApiResult>(this.baseUrl)
    }

    addHierarchy(add: HierarchyAdd): Observable<HierarchyApiResult> {
        return this.http.post<HierarchyApiResult>(this.baseUrl, add)
    }

    updateHierarchy(update: Hierarchy): Observable<HierarchyApiResult> {
        return this.http.put<HierarchyApiResult>(this.baseUrl, update)
    }
}
