/*export interface User {
    username: string;
    token: string;
}*/

//Interface for User Model
export interface User {
    id?: string;
    userName?: string;
    lastName?: string;
    firstName?: string;
    department?: string;
    roleId?: string;
    roleName?: string;
    roleSelected: {},
    hierarchiesId: [],
    hierarchyName?: string;
    active?: string
}


//Interface for Used Data Model
export interface UserData {
    roles: UserRole[];
    error: string;
    hierarchy: string;
    usersData: User[];
}


//Interface for User Role Model
export interface UserRole { 
    id: string;
    name: string;
}
