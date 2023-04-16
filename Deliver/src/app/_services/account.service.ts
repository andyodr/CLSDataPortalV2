import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { BehaviorSubject, tap } from "rxjs"
import { UserState, AuthenticatedUser } from "../_models/user"
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
    private currentUserSource = new BehaviorSubject<UserState | null>(null)
    currentUser$ = this.currentUserSource.asObservable()

    constructor(private http: HttpClient, private router: Router) { }

    login(model: SignIn) {
        var form = new FormData()
        form.append("userName", model.userName)
        form.append("password", model.password)
        form.append("persistent", model.persistent.toString())
        return this.http.post<AuthenticatedUser>(environment.baseUrl + "api/SignIn", form)
    }

    setCurrentUser(user: UserState) {
        this.currentUserSource.next(user)
    }

    getCurrentUser() {
        return this.currentUserSource.value;
    }

    saveSettings(settings: { [key: string]: any }) {
        if (!this.currentUserSource.value) return
        Object.assign(this.currentUserSource.value.filter, settings)
        localStorage.setItem("userState", JSON.stringify(this.currentUserSource.value))
    }

    getFilters() {
        return this.currentUserSource.value?.filter
    }

    /** Perform SignOut and navigate to SignIn screen */
    logout() {
        this.http.get(environment.baseUrl + "api/SignOut", { observe: "response" })
            .subscribe({
                next: result => {
                    if (result.status == 200) {
                        if (!this.getCurrentUser()?.persist) {
                            localStorage.removeItem("userState")
                            this.currentUserSource.next(null)
                        }

                        this.router.navigateByUrl("/")
                    }
                    else {
                        console.error(`Unexpected status: ${result.status}`)
                    }
                },
                error: () => {
                    console.error("Signout failed")
                }
            })
    }
}
