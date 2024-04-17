import { Component, AfterViewInit, DestroyRef, inject } from "@angular/core"
import { Router } from "@angular/router"
import { NavSettingsService } from "src/app/_services/nav-settings.service"
import { AccountService, SignIn } from "../../_services/account.service"
import { LoggerService } from "../../_services/logger.service"
import { finalize } from "rxjs"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { UserState } from "src/app/_models/user"
import { MatButtonModule } from "@angular/material/button"
import { MatTooltipModule } from "@angular/material/tooltip"
import { MatCheckboxModule } from "@angular/material/checkbox"
import { MatInputModule } from "@angular/material/input"
import { MatFormFieldModule } from "@angular/material/form-field"
import { FormsModule } from "@angular/forms"
import { MatProgressBarModule } from "@angular/material/progress-bar"

@Component({
    selector: "app-login",
    templateUrl: "./login.component.html",
    styleUrls: ["./login.component.css"],
    standalone: true,
    imports: [MatProgressBarModule, FormsModule, MatFormFieldModule, MatInputModule, MatCheckboxModule, MatTooltipModule, MatButtonModule]
})
export class LoginComponent implements AfterViewInit {

    model: SignIn = { userName: "", password: "", persistent: false }
    progress = false
    canConnect = false
    destroyRef = inject(DestroyRef)
    timeoutId!: any

    constructor(private acctSvc: AccountService, private router: Router, private logger: LoggerService, public _navSettingsService: NavSettingsService) {
        _navSettingsService.hideNavBar()
        const user = acctSvc.getCurrentUser()
        if (user?.persist) {
            this.model.userName = user.userName
            this.model.persistent = user.persist
        }
    }

    ngAfterViewInit() {
        if (document.cookie.split(";").some(c => c.trim().startsWith("AuthPresent="))) {
            setTimeout(() => {
                this._navSettingsService.showNavBar()
                this.router.navigate(["measuredata"])
            }, 250)
        }
        else {
            this.updateDatabaseStatus()
        }
}

    login() {
        this.progress = true
        this.acctSvc.login(this.model)
            .pipe(finalize(() => this.progress = false), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: user => {
                    try {
                        const userState = user as UserState
                        userState.filter = { measureTypeId: 0, hierarchyId: 0, year: new Date().getFullYear() }
                        const stored = localStorage.getItem("userState")
                        if (stored) {
                            const parsed = JSON.parse(stored) as UserState
                            if (parsed.id === userState.id) {
                                userState.filter = parsed.filter
                            }
                        }

                        this.acctSvc.setCurrentUser(userState)
                        localStorage.setItem("userState", JSON.stringify(userState))
                    }
                    catch { }
                    this._navSettingsService.showNavBar()
                    this.router.navigate(["measuredata"])
                    clearTimeout(this.timeoutId)
                },
                error: error => this.logger.logError(error.message)
            })
    }

    logout() {
        this.acctSvc.logout()
    }

    clear() {
        if (!this.model.persistent && !this.model.password) {
            this.model.userName = ""
        }
    }

    updateDatabaseStatus() {
        this.acctSvc.checkApiStatus().subscribe(result => {
            this.canConnect = result.databaseConnected
            this.timeoutId = setTimeout(this.updateDatabaseStatus.bind(this), 6_000)
        })
    }
}
