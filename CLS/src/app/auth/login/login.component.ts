import { Component, EventEmitter, OnInit, Output } from "@angular/core"
import { Router } from "@angular/router"
import { AuthenticatedUser, User } from "src/app/_models/user"
import { NavSettingsService } from "src/app/_services/nav-settings.service"
import { AccountService, SignIn } from "../../_services/account.service"
import { LoggerService } from "../../_services/logger.service"

@Component({
    selector: "app-login",
    templateUrl: "./login.component.html",
    styleUrls: ["./login.component.css"]
})
export class LoginComponent implements OnInit {

    @Output() cancelLogin = new EventEmitter();
    model: SignIn = { userName: "", password: "", persistent: false }

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

    login() {
        this.api.login(this.model).subscribe({
            next: user => {
                try {
                    localStorage.setItem("user", JSON.stringify(user))
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
        this.router.navigateByUrl("/")
    }

    cancel() {
        this.cancelLogin.emit(false)
    }
}
