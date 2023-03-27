import { ErrorModel } from "./error"
import { RegionFilter } from "./regionhierarchy"

//Interface for User Model
export interface User {
    id?: number
    userName: string
    lastName?: string
    firstName?: string
    department?: string
    roleId: number
    hierarchiesId: number[]
    hierarchyName?: string
    active?: string
}

export interface AuthenticatedUser extends User {
    persist: boolean
}

export interface RolesAndRegions {
    hierarchy: RegionFilter[]
    roles: UserRole[]
    error: ErrorModel
}

export interface UserData extends RolesAndRegions {
    data: User[]
}

export interface UserRole {
    id: string
    name: string
}
