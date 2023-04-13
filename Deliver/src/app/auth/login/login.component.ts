import { Component, EventEmitter, OnInit, AfterViewInit, Output } from "@angular/core"
import { Router } from "@angular/router"
import { NavSettingsService } from "src/app/_services/nav-settings.service"
import { AccountService, SignIn } from "../../_services/account.service"
import { LoggerService } from "../../_services/logger.service"
import { finalize } from "rxjs"
import { UserState } from "src/app/_models/user"

@Component({
    selector: "app-login",
    templateUrl: "./login.component.html",
    styleUrls: ["./login.component.css"]
})
export class LoginComponent implements OnInit, AfterViewInit {

    @Output() cancelLogin = new EventEmitter();
    model: SignIn = { userName: "", password: "", persistent: false }
    progress = false

    constructor(
        private api: AccountService,
        private router: Router,
        private logger: LoggerService,
        public _navSettingsService: NavSettingsService) { }

    ngOnInit() {
        this._navSettingsService.hideNavBar()
        const user = this.api.getCurrentUser()
        if (user?.persist) {
            this.model.userName = user.userName
            this.model.persistent = user.persist
        }
    }

    ngAfterViewInit() {
        if (document.cookie.split(";").some(c => c.trim().startsWith("AuthPresent="))) {
            setTimeout(() => {
                this.logger.logInfo("Proceed")
                this._navSettingsService.showNavBar()
                this.router.navigate(["measuredata"])
            }, 500)
        }
    }

    login() {
        this.progress = true
        this.api.login(this.model)
            .pipe(finalize(() => this.progress = false))
            .subscribe({
                next: user => {
                    try {
                        const userState = user as UserState
                        userState.filter = {}
                        const u = localStorage.getItem("userState")
                        if (u) {
                            const p = JSON.parse(u) as UserState
                            if (p.id === user.id) {
                                userState.filter = { measureTypeId: p.filter?.measureTypeId }
                            }
                        }

                        this.api.setCurrentUser(userState)
                        localStorage.setItem("userState", JSON.stringify(userState))
                    }
                    catch { }
                    this._navSettingsService.showNavBar()
                    this.router.navigate(["measuredata"])
                },
                error: error => this.logger.logError(error.message)
            })
    }

    logout() {
        this.api.logout()
    }

    cancel() {
        this.cancelLogin.emit(false)
    }
}
