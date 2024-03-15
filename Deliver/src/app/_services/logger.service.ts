import { Component, Inject, Injectable } from "@angular/core"
import { MatSnackBar, MatSnackBarConfig, MAT_SNACK_BAR_DATA } from "@angular/material/snack-bar"
import { MatIconModule } from "@angular/material/icon"

type PanelClass = "snackbar-info" | "snackbar-warning" | "snackbar-success" | "snackbar-error"

@Component({
    template: `<div style="display: flex; align-items: middle;" >
    <mat-icon>{{icon}}</mat-icon>
    <span style="margin: 3px 6px 0px;">{{message}}</span></div>`,
    standalone: true,
    imports: [MatIconModule]
})
export class SbComponent {
    constructor(@Inject(MAT_SNACK_BAR_DATA) public sbData: any) { }
    get icon() {
        return this.sbData.icon
    }

    get message() {
        return this.sbData.message
    }
}

@Injectable({ providedIn: "root" })
export class LoggerService {
    cfg: MatSnackBarConfig = {
        duration: 5000,
        horizontalPosition: "right",
        verticalPosition: "bottom"
    }

    constructor(private snackbar: MatSnackBar) { }

    logIt(message: string, panelClass: PanelClass, icon: string) {
        this.snackbar.openFromComponent(SbComponent, {
            panelClass: [panelClass],
            data: { message, icon },
            ...this.cfg
        })
    }

    logInfo(message: string) {
        this.logIt(message, "snackbar-info", "info")
    }

    logWarning(message: string) {
        this.logIt(message, "snackbar-warning", "report_problem")
    }

    logSuccess(message: string) {
        this.logIt(message, "snackbar-success", "check")
    }

    logError(message: string) {
        this.logIt(message, "snackbar-error", "report")
    }
}
