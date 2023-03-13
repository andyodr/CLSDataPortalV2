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

export interface Hierarchy {
    id: number
    levelId?: number
    level: string
    name: string
    parentId?: string
    parentName: string
    active?: boolean
    remove?: boolean
}

export type HierarchyApiResult = {
    data: Hierarchy[]
    hierarchy: RegionFilter[]
    regionId: number
    levels: { id: number, name: string }[]
    error: ErrorModel
}
