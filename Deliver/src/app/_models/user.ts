import { RegionFilter } from "../_services/hierarchy.service"
import { ErrorModel } from "./error"

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
    active?: boolean
}

export interface AuthenticatedUser extends User {
    persist: boolean
}

export interface UserState extends AuthenticatedUser {
    filter: { measureTypeId: number, hierarchyId: number, year: number }
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
