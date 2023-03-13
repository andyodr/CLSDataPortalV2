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
