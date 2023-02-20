import { HttpClient } from '@angular/common/http'
import { Injectable } from '@angular/core'
import { BehaviorSubject, map } from 'rxjs'
import { User } from '../_models/user'
import { environment } from "../environments/environment"

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private currentUserSource = new BehaviorSubject<User | null>(null)
  currentUser$ = this.currentUserSource.asObservable()

  constructor(private http: HttpClient) { }

  login(model: any) {
    var form = new FormData()
    form.append("userName", model.username)
    form.append("password", model.password)
    return this.http.post<User>(environment.baseUrl + 'api/SignIn', form).pipe(
      map((response: User) => {
        const user = response
        if (user) {
          localStorage.setItem('user', JSON.stringify(user))
          this.currentUserSource.next(user)
        }
      })
    )
  }

  register(model: any) {
    return this.http.post<User>(environment.baseUrl + 'account/register', model).pipe(
      map(user => {
        if (user) {
          localStorage.setItem('user', JSON.stringify(user))
          this.currentUserSource.next(user)
        }
      })
    )
  }

  setCurrentUser(user: User) {
    this.currentUserSource.next(user);
  }

  logout() {
    this.http.get(environment.baseUrl + "api/SignOut", { observe: "response" })
      .subscribe(result => {
        if (result.status == 200) {
          localStorage.removeItem('user')
          this.currentUserSource.next(null)
        }
        else {
          console.error(`Unexpected status: {result.status}`)
        }
      }, () => {
        console.error("Signout failed")
      })
  }
}
