import { ErrorModel } from "./error"
import { RegionFilter } from "./regionfilter"

//Interface for User Model
export interface User {
    id?: string
    userName: string
    lastName?: string
    firstName?: string
    department?: string
    roleId: number
    roleName: string
    hierarchiesId: number[]
    hierarchyName?: string
    active?: string
}

//Interface for Used Data Model
export interface UserData {
    roles: UserRole[]
    error: ErrorModel
    hierarchy: RegionFilter[]
    data: User[]
}

//Interface for User Role Model
export interface UserRole {
    id: string
    name: string
}
