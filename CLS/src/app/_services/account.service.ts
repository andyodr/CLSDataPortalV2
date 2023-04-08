import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { BehaviorSubject, tap } from "rxjs"
import { AuthenticatedUser } from "../_models/user"
import { environment } from "../../environments/environment"
import { Router } from "@angular/router"

export type SignIn = {
    userName: string,
    password: string,
    persistent: boolean
}

@Injectable({
    providedIn: "root"
})
export class AccountService {
    private currentUserSource = new BehaviorSubject<AuthenticatedUser | null>(null)
    currentUser$ = this.currentUserSource.asObservable()

    constructor(private http: HttpClient) { }

    login(model: SignIn) {
        var form = new FormData()
        form.append("userName", model.userName)
        form.append("password", model.password)
        form.append("persistent", model.persistent.toString())
        return this.http.post<AuthenticatedUser>(environment.baseUrl + "api/SignIn", form)
            .pipe(tap(user => this.currentUserSource.next(user)))
    }

    setCurrentUser(user: AuthenticatedUser) {
        this.currentUserSource.next(user)
    }

    getCurrentUser() {
        return this.currentUserSource.value;
    }

    logout() {
        this.http.get(environment.baseUrl + "api/SignOut", { observe: "response" })
            .subscribe({
                next: result => {
                    if (result.status == 200) {
                        if (!this.getCurrentUser()?.persist) {
                            localStorage.removeItem("user")
                        }

                        this.currentUserSource.next(null)
                    }
                    else {
                        console.error(`Unexpected status: {result.status}`)
                    }
                },
                error: () => {
                    console.error("Signout failed")
                }
            })
    }
}
