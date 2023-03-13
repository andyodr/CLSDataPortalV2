import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { RolesAndRegions, User, UserData } from '../_models/user';

@Injectable({ providedIn: "root" })
export class UserService {
    private baseUrl = environment.baseUrl + 'api/users/';
    /*users: User[] = [];
    userData: UserData | undefined;
    users$: Observable<User[]> | undefined;*/

    constructor(private http: HttpClient) { }

    // Get Users from API
    getUsers(): Observable<User[]> {
        //return this.http.get<User[]>(environment.baseUrl + 'api/users/index');
        return this.http.get<User[]>(this.baseUrl + '/index').pipe(
            map((response: User[]) => {
                const usersOnService = response
                console.log("Users On Service : ", usersOnService);
                return usersOnService
            }),
        );
    }

    getUserData(id: number) {
        return this.http.get<UserData>(`${this.baseUrl}edit/${id}`)
    }

    addUser(user: User): Observable<UserData> {
        return this.http.post<UserData>(`${this.baseUrl}add`, user)
    }

    updateUser(user: User): Observable<User> {
        return this.http.put<User>(`${this.baseUrl}edit/${user.id}`, user)
    }

    getRolesAndRegions() {
        return this.http.get<RolesAndRegions>(`${this.baseUrl}add`)
    }

    //el tipo
    /*getUsers():Observable<User[]>{
      //return this.http.get<User[]>(environment.baseUrl + 'api/users/index');
      return this.http.get<User[]>(this.baseUrl + '/index').pipe(
        map(response => ({
          const allUsersData = response
          console.log("All Users Data : ", allUsersData);
          this.users = response 
        })
      );
    }*/

    //el tipo cambiado

    /*getUsers(){
      //return this.http.get<User[]>(environment.baseUrl + 'api/users/index');
      return this.http.get<User[]>(this.baseUrl + '/index').subscribe(
        response => {
          this.users = response.data
          const allUsersData = response.data
          console.log("All Users Data : ", allUsersData);
          this.users = response.data
        });
    }*/


    /*getUsers(): Observable<User[]>{
      //return this.http.get<User[]>(environment.baseUrl + 'api/users/index');
      return this.http.get<User[]>(this.baseUrl + '/index').pipe(
        map((response: User[]) => { this.users = response })
      );
    }*/


    /*getUser(id: number): Observable<User> {
      return this.http.get<User>(`${this.baseUrl}/${id}`);
    }
  
    deleteUser(id: number): Observable<void> {
      return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
  
  
    /*getById(id: string) {
        return this.http.get<User>(`${baseUrl}/${id}`);
    }
  
    create(params: any) {
        return this.http.post(baseUrl, params);
    }
  
    update(id: string, params: any) {
        return this.http.put(`${baseUrl}/${id}`, params);
    }
  
    delete(id: string) {
        return this.http.delete(`${baseUrl}/${id}`);
    }*/
}
