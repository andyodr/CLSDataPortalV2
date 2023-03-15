import { ErrorModel } from "./error"

export type RegionFilter = {
    hierarchy: string
    id: number
    count?: number
    sub: RegionFilter[]
    found?: boolean
    error: ErrorModel
}

export class RegionFlatNode {
    hierarchy!: string
    level!: number
    expandable!: boolean
}

export interface HierarchyAdd {
    levelId: number
    name: string
    parentId: number
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
