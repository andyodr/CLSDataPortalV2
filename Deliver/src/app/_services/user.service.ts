import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { RolesAndRegions, User, UserData } from '../_models/user';

@Injectable({ providedIn: "root" })
export class UserService {
    private baseUrl = environment.baseUrl + 'api/users/';
    /*users: User[] = [];
    userData: UserData | undefined;
    users$: Observable<User[]> | undefined;*/

    constructor(private http: HttpClient) { }

    // Get Users from API
    getUsers() {
        return this.http.get<UserData>(this.baseUrl + '/index')
    }

    getUserData(id: number) {
        return this.http.get<UserData>(`${this.baseUrl}edit/${id}`)
    }

    addUser(user: User) {
        return this.http.post<UserData>(`${this.baseUrl}add`, user)
    }

    updateUser(user: User) {
        return this.http.put<UserData>(`${this.baseUrl}edit/${user.id}`, user)
    }

    getRolesAndRegions() {
        return this.http.get<RolesAndRegions>(`${this.baseUrl}add`)
    }
}
